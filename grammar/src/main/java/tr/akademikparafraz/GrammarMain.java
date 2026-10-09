package tr.akademikparafraz;
import com.google.gson.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import org.languagetool.JLanguageTool;
import org.languagetool.Languages;
import org.languagetool.rules.RuleMatch;
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
