package tr.akademikparafraz;
import com.google.gson.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.stream.Collectors;
import zemberek.morphology.TurkishMorphology;
import zemberek.morphology.analysis.SingleAnalysis;
import zemberek.morphology.lexicon.DictionaryItem;

/** Single-threaded UTF-8 stdio protocol: no network listener, no text logging. */
public final class NlpMain {
 public static void main(String[] args)throws Exception{
  TurkishMorphology morphology=TurkishMorphology.createWithDefaults();
  PrintWriter out=new PrintWriter(new OutputStreamWriter(System.out,StandardCharsets.UTF_8),true);
  out.println("{\"ready\":true,\"engine\":\"Zemberek 0.17.1\"}");
  BufferedReader input=new BufferedReader(new InputStreamReader(System.in,StandardCharsets.UTF_8));String line;
  while((line=input.readLine())!=null){
   try{
    if(line.length()>400000){out.println("{\"error\":\"request_too_large\"}");continue;}
    JsonObject req=JsonParser.parseString(line).getAsJsonObject();String op=req.get("op").getAsString();
    if(op.equals("shutdown"))break;
    if(op.equals("analyze")){
     String text=req.get("text").getAsString();JsonArray tokens=new JsonArray();int cursor=0;
     for(SingleAnalysis a:morphology.analyzeAndDisambiguate(text).bestAnalysis()){
      String surface=a.surfaceForm();if(surface.isEmpty())continue;int start=text.indexOf(surface,cursor);
      if(start<0){surface=surface.toLowerCase(new Locale("tr","TR"));start=text.toLowerCase(new Locale("tr","TR")).indexOf(surface,cursor);}
      if(start<0)continue;cursor=start+surface.length();DictionaryItem item=a.getDictionaryItem();JsonObject t=new JsonObject();
      t.addProperty("Start",start);t.addProperty("Length",surface.length());t.addProperty("Surface",text.substring(start,cursor));t.addProperty("Lemma",item.lemma);t.addProperty("Pos",a.getPos().name());
      t.addProperty("Proper",item.secondaryPos.name().equals("ProperNoun")||item.secondaryPos.name().equals("Abbreviation"));JsonArray ms=new JsonArray();a.getMorphemes().forEach(m->ms.add(m.id));t.add("Morphemes",ms);tokens.add(t);
     }JsonObject result=new JsonObject();result.add("tokens",tokens);out.println(result);
    }else if(op.equals("inflect")){
     String source=req.get("source").getAsString(),target=req.get("target").getAsString(),pos=req.get("pos").getAsString();Set<String> outputs=new TreeSet<>();
     List<SingleAnalysis> analyses=new ArrayList<>();morphology.analyze(source).forEach(analyses::add);
     // Never guess among incompatible suffix analyses. Every compatible analysis must agree.
     for(SingleAnalysis a:analyses){if(!a.getPos().name().equals(pos)||a.getDictionaryItem().secondaryPos.name().equals("ProperNoun"))continue;
      for(DictionaryItem item:morphology.getLexicon().getMatchingItems(target)){
       if(!item.primaryPos.name().equals(pos))continue;List<zemberek.morphology.morphotactics.Morpheme> suffixes=a.getMorphemes().stream().skip(1).collect(Collectors.toList());
       morphology.getWordGenerator().generate(item,suffixes).forEach(r->outputs.add(r.surface));
      }
     }JsonObject result=new JsonObject();if(outputs.size()==1)result.addProperty("surface",outputs.iterator().next());else result.add("surface",JsonNull.INSTANCE);out.println(result);
    }else out.println("{\"error\":\"unknown_operation\"}");
   }catch(Exception ex){out.println("{\"error\":\"nlp_operation_failed\"}");}
  }
 }
}
