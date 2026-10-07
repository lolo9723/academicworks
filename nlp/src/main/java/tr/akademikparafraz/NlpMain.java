package tr.akademikparafraz;
import com.google.gson.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.stream.Collectors;
import zemberek.morphology.TurkishMorphology;
import zemberek.morphology.analysis.SingleAnalysis;
import zemberek.morphology.analysis.SentenceWordAnalysis;
import zemberek.morphology.lexicon.DictionaryItem;
import zemberek.morphology.morphotactics.Morpheme;
import zemberek.morphology.morphotactics.TurkishMorphotactics;

/** Single-threaded UTF-8 stdio protocol: no network listener, no text logging. */
public final class NlpMain {
 public static void main(String[] args)throws Exception{
  TurkishMorphology morphology=TurkishMorphology.createWithDefaults();
  PrintWriter out=new PrintWriter(new OutputStreamWriter(System.out,StandardCharsets.UTF_8),true);
  out.println("{\"ready\":true,\"engine\":\"Zemberek 0.17.1\",\"protocol\":2}");
  BufferedReader input=new BufferedReader(new InputStreamReader(System.in,StandardCharsets.UTF_8));String line;
  while((line=input.readLine())!=null){
   try{
    if(line.length()>400000){out.println("{\"error\":\"request_too_large\"}");continue;}
    JsonObject req=JsonParser.parseString(line).getAsJsonObject();String op=req.get("op").getAsString();
    if(op.equals("shutdown"))break;
    if(op.equals("analyze")){
     String text=req.get("text").getAsString();JsonArray tokens=new JsonArray();int cursor=0;
     Locale tr=new Locale("tr","TR");String folded=text.toLowerCase(tr);
     for(SentenceWordAnalysis entry:morphology.analyzeAndDisambiguate(text)){
      SingleAnalysis a=entry.bestAnalysis;
      // Use the actual tokenizer input, not a reconstructed stem/suffix surface.
      // Always find the earliest case-insensitive occurrence: a lowercase exact
      // search first could jump past the initial capitalized occurrence.
      String surface=entry.wordAnalysis.getInput();if(surface.isEmpty())continue;
      int start=folded.indexOf(surface.toLowerCase(tr),cursor);
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
    }else if(op.equals("structure")){
     String source=req.get("source").getAsString(),mode=req.get("mode").getAsString();
     Set<String> outputs=new TreeSet<>();
     if(source.length()<=100 && source.matches("[\\p{L}\\p{M}]+")){
      for(SingleAnalysis a:morphology.analyze(source)){
       DictionaryItem item=a.getDictionaryItem();
       if(item.secondaryPos.name().equals("ProperNoun")||item.secondaryPos.name().equals("Abbreviation"))continue;
       if(req.has("lemma")&&!item.lemma.equals(req.get("lemma").getAsString()))continue;
       if(req.has("morphemes")){
        List<String> expected=new ArrayList<>();req.getAsJsonArray("morphemes").forEach(m->expected.add(m.getAsString()));
        if(!a.getMorphemes().stream().map(m->m.id).collect(Collectors.toList()).equals(expected))continue;
       }
       List<Morpheme> suffixes=new ArrayList<>(a.getMorphemes().subList(1,a.getMorphemes().size()));
       if(mode.equals("Gen")){
        if(!a.getPos().name().equals("Noun")||suffixes.isEmpty())continue;
        Set<String> cases=new HashSet<>(Arrays.asList("Nom","Acc","Dat","Gen","Abl","Loc","Ins","Equ"));
        String last=suffixes.get(suffixes.size()-1).id;
        // Licensed only for the nominative subject of a procedural clause.
        // Do not reinterpret an accusative reading as a possessive nominative.
        if(cases.contains(last)&&!last.equals("Nom"))continue;
        if(last.equals("Nom"))suffixes.set(suffixes.size()-1,TurkishMorphotactics.gen);
        else suffixes.add(TurkishMorphotactics.gen); // SingleAnalysis omits zero-surface Nom.
       }else if(mode.equals("Passive")){
        if(!a.getPos().name().equals("Verb")||!item.primaryPos.name().equals("Verb"))continue;
        if(suffixes.stream().anyMatch(m->Arrays.asList("Pass","While","PastPart","FutPart","Inf1","Inf2","Inf3").contains(m.id)))continue;
        // Derivations precede negation, tense and person; these are retained intact.
        int insertion=0;
        while(insertion<suffixes.size() && Arrays.asList("Caus","Recip","Reflex").contains(suffixes.get(insertion).id))insertion++;
        suffixes.add(insertion,TurkishMorphotactics.pass);
       }else if(mode.equals("Nominal")){
        if(!item.primaryPos.name().equals("Verb")||suffixes.stream().noneMatch(m->m.id.equals("While")))continue;
        // Temporal procedural passives only. Active/negative 'while' may be contrastive.
        if(suffixes.stream().noneMatch(m->m.id.equals("Pass"))||suffixes.stream().anyMatch(m->Arrays.asList("Neg","Able","Unable").contains(m.id)))continue;
        List<Morpheme> stem=new ArrayList<>();
        for(Morpheme m:suffixes){if(Arrays.asList("Caus","Pass","Recip","Reflex","Verb").contains(m.id))stem.add(m);else break;}
        stem.add(TurkishMorphotactics.inf2);stem.add(TurkishMorphotactics.a3sg);stem.add(TurkishMorphotactics.p3sg);stem.add(TurkishMorphotactics.nom);suffixes=stem;
       }else continue;
       final List<Morpheme> requested=suffixes;
       Map<String,List<String>> sameReading=new TreeMap<>();
       morphology.getWordGenerator().generate(item,suffixes).forEach(r->{
        long pass=requested.stream().filter(m->m.id.equals("Pass")).count();
        if(r.analysis.getMorphemes().stream().filter(m->m.id.equals("Pass")).count()!=pass)return;
        String key=r.analysis.formatLexical();
        sameReading.computeIfAbsent(key,k->new ArrayList<>()).add(r.surface);
       });
       // 'incelenmesi/incelenilmesi' can be two surfaces of the SAME
       // passive analysis. Prefer its shortest standard allomorph. Different
       // morphological readings still have to agree; never choose between them.
       for(List<String> forms:sameReading.values()){
        int shortest=forms.stream().mapToInt(String::length).min().orElse(0);
        forms.stream().filter(s->s.length()==shortest).forEach(outputs::add);
       }
      }
     }
     JsonObject result=new JsonObject();if(outputs.size()==1)result.addProperty("surface",outputs.iterator().next());else result.add("surface",JsonNull.INSTANCE);out.println(result);
    }else out.println("{\"error\":\"unknown_operation\"}");
   }catch(Exception ex){out.println("{\"error\":\"nlp_operation_failed\"}");}
  }
 }
}
