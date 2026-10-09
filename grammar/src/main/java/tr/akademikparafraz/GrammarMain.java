package tr.akademikparafraz;
import com.google.gson.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import org.languagetool.JLanguageTool;
import org.languagetool.Languages;
import org.languagetool.rules.RuleMatch;
import org.languagetool.AnalyzedTokenReadings;
import org.languagetool.AnalyzedToken;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
/** Local UTF-8 protocol; no server, network or text log. Dependencies remain separate JARs. */
public final class GrammarMain {
 public static void main(String[] args)throws Exception {
  JLanguageTool checker=new JLanguageTool(Languages.getLanguageForShortCode("en-US"));
  PrintWriter out=new PrintWriter(new OutputStreamWriter(System.out,StandardCharsets.UTF_8),true);
  BufferedReader in=new BufferedReader(new InputStreamReader(System.in,StandardCharsets.UTF_8));
  out.println("{\"ready\":true,\"engine\":\"LanguageTool 6.6 English\"}");String line;
  while((line=in.readLine())!=null){try{
   if(line.length()>400000){out.println("{\"error\":\"request_too_large\"}");continue;}
   JsonObject request=JsonParser.parseString(line).getAsJsonObject();
   if(request.has("shutdown"))break;
   if(request.has("lemmaWords")){
    JsonArray words=request.getAsJsonArray("lemmaWords");if(words.size()>200)throw new IllegalArgumentException("Too many dictionary words");
    List<String> surfaces=new ArrayList<>();for(JsonElement word:words){String value=word.getAsString();if(value.length()>100)throw new IllegalArgumentException("Word too long");surfaces.add(value.toLowerCase(Locale.ROOT));}
    List<AnalyzedTokenReadings> tagged=checker.getLanguage().getTagger().tag(surfaces);JsonArray entries=new JsonArray();
    if(tagged.size()!=surfaces.size())throw new IllegalStateException("Dictionary response length");
    for(AnalyzedTokenReadings readings:tagged){JsonArray variants=new JsonArray();for(AnalyzedToken reading:readings.getReadings()){
      if(reading.getLemma()!=null&&reading.getPOSTag()!=null&&reading.getPOSTag().startsWith("VB")){JsonObject variant=new JsonObject();variant.addProperty("lemma",reading.getLemma());variant.addProperty("pos",reading.getPOSTag());variants.add(variant);}
    }entries.add(variants);}
    JsonObject response=new JsonObject();response.add("lemmaEntries",entries);out.println(response);continue;
   }
   String text=request.get("text").getAsString();JsonArray issues=new JsonArray();
   for(RuleMatch m:checker.check(text)){
    JsonObject issue=new JsonObject();issue.addProperty("Start",m.getFromPos());issue.addProperty("Length",m.getToPos()-m.getFromPos());
    issue.addProperty("Code",m.getRule().getId());issue.addProperty("Message",m.getMessage());
    issue.addProperty("Kind",m.getRule().getLocQualityIssueType().toString());issues.add(issue);
   }
   JsonObject response=new JsonObject();response.add("issues",issues);out.println(response);
  }catch(Exception e){out.println("{\"error\":\"grammar_failed\"}");}}
 }
}
