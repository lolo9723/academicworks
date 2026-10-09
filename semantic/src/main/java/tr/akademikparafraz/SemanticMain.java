package tr.akademikparafraz;
import ai.djl.huggingface.tokenizers.Encoding;
import ai.djl.huggingface.tokenizers.HuggingFaceTokenizer;
import ai.onnxruntime.*;
import com.google.gson.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.Path;
import java.util.*;

/** Local classification only. No network, document persistence or free text generation. */
public final class SemanticMain {
  private static final Gson JSON=new Gson();
  public static void main(String[] args) throws Exception {
    if(args.length!=2)throw new IllegalArgumentException("model tokenizer");
    try(var environment=OrtEnvironment.getEnvironment();var options=new OrtSession.SessionOptions();
        var tokenizer=HuggingFaceTokenizer.builder().optTokenizerPath(Path.of(args[1])).optTruncation(false).optPadding(false).build()) {
      options.setIntraOpNumThreads(1);options.setInterOpNumThreads(1);
      try(var session=environment.createSession(args[0],options);
          var reader=new BufferedReader(new InputStreamReader(System.in,StandardCharsets.UTF_8));
          var writer=new PrintWriter(new OutputStreamWriter(System.out,StandardCharsets.UTF_8),true)) {
        writer.println(JSON.toJson(Map.of("ready",true,"component","mDeBERTa ONNX CPU local NLI","nativeLoaderPatch",ai.djl.huggingface.tokenizers.jni.LibUtils.academicPatch())));
        String line;
        while((line=reader.readLine())!=null) {
          try {
            if(line.length()>80000)throw new IllegalArgumentException("Request too large");
            var request=JsonParser.parseString(line).getAsJsonObject();
            String source=request.get("source").getAsString(),target=request.get("target").getAsString();
            if(source.isBlank()||target.isBlank())throw new IllegalArgumentException("Empty pair");
            float[] forward=infer(environment,session,tokenizer,source,target);
            float[] reverse=infer(environment,session,tokenizer,target,source);
            writer.println(JSON.toJson(Map.of("forward",forward,"reverse",reverse)));
          } catch(Exception e) { writer.println("{\"error\":\"LOCAL_SEMANTIC_CHECK_FAILED\"}"); }
        }
      }
    }
  }
  private static float[] infer(OrtEnvironment env,OrtSession session,HuggingFaceTokenizer tokenizer,String a,String b) throws Exception {
    Encoding encoding=tokenizer.encode(a,b);
    if(encoding.getIds().length>512||encoding.getOverflowing().length>0)throw new IllegalArgumentException("No silent truncation");
    Map<String,OnnxTensor> tensors=new HashMap<>();
    try {
      tensors.put("input_ids",OnnxTensor.createTensor(env,new long[][]{encoding.getIds()}));
      tensors.put("attention_mask",OnnxTensor.createTensor(env,new long[][]{encoding.getAttentionMask()}));
      if(session.getInputNames().contains("token_type_ids"))tensors.put("token_type_ids",OnnxTensor.createTensor(env,new long[][]{encoding.getTypeIds()}));
      try(var prediction=session.run(tensors)) {
        float[] logits=((float[][])prediction.get(0).getValue())[0];
        if(logits.length!=3)throw new IllegalStateException("Unexpected labels");
        double max=Math.max(logits[0],Math.max(logits[1],logits[2])),sum=0;float[] scores=new float[3];
        for(int i=0;i<3;i++){scores[i]=(float)Math.exp(logits[i]-max);sum+=scores[i];}
        for(int i=0;i<3;i++)scores[i]/=(float)sum;
        return scores; // pinned model: entailment, neutral, contradiction
      }
    } finally { for(var tensor:tensors.values())tensor.close(); }
  }
}
