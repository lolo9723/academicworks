#!/usr/bin/env python3
"""Regenerate the reviewed sentence frames and exact technical-term seed additions.
Counts are finite template forms, not independent syntactic families.
Run explicitly when editing this source; review and commit its JSON output.
"""
from pathlib import Path
import json
root=Path(__file__).resolve().parent.parent
rules=[]
def add(pattern,target,hint='',strength=2):
 rules.append(dict(Id=f'sentence-v1-{len(rules)+1:04}',Pattern=pattern,Target=target,Family='cümle kuruluşu',Strength=strength,Confidence=.96,Domain='genel',RequiredText=hint))
# A full goal is captured unchanged, including its object cases. No guessed nominalization.
body=r'(?<goal>[^.!?;\r\n\a]{3,1100}?)'
for work in ['çalışma','araştırma','inceleme']:
 poss = 'nin' if work == 'inceleme' else 'nın'
 loc = 'de' if work == 'inceleme' else 'da'
 for intro in ['bu ','mevcut ','']:
  for ending,acc in [('maktır','mayı'),('mektir','meyi')]:
   pattern=r'\b'+intro+work+poss+r' amacı,? '+body+ending+r'\b'
   for target in [intro+work+', ${goal}'+acc+' amaçlamaktadır',intro+work+', ${goal}'+acc+' hedeflemektedir']:
    add(pattern,target,'amacı')
 for marker in ['ikincil','birincil','temel','diğer']:
  for ending,acc in [('maktır','mayı'),('mektir','meyi')]:
   pattern=r'\b(?:bu )?'+work+poss+" "+marker+r' amacı,? (?:ise )?'+body+ending+r'\b'
   add(pattern,work+', '+marker+' amaç doğrultusunda ${goal}'+acc+' hedeflemektedir','amacı')
   add(pattern,marker+' amaç olarak, '+work+' ${goal}'+acc+' hedeflemektedir','amacı')
# Reorder a reporting frame while retaining the operation, tense, negation and topic.
verbs=['incelenmiştir','incelenmemiştir','incelenmektedir','incelenmemektedir',
       'karşılaştırılmıştır','karşılaştırılmamıştır','karşılaştırılmaktadır',
       'değerlendirilmiştir','değerlendirilmemiştir','değerlendirilmektedir',
       'analiz edilmiştir','analiz edilmemiştir','analiz edilmektedir',
       'sınıflandırılmıştır','sınıflandırılmamıştır','sınıflandırılmaktadır',
       'toplanmıştır','toplanmamıştır','toplanmaktadır',
       'kaydedilmiştir','kaydedilmemiştir','ölçülmüştür','ölçülmemiştir',
       'hesaplanmıştır','hesaplanmamıştır','belirlenmiştir','belirlenmemiştir']
obj=r'(?<object>[^.!?;\r\n\a]{3,1000}?)'
scope=r'(?<scope>(?:bu|mevcut|sunulan) (?:çalışmada|araştırmada|incelemede))'
for verb in verbs:
 add(r'\b'+scope+' '+obj+' '+verb+r'\b','${object}, ${lower:scope} '+verb,verb,3)
 for method in ['retrospektif olarak','prospektif olarak','nitel olarak','nicel olarak','sistematik olarak']:
  add(r'\b'+obj+' '+method+' '+verb+r'\b',method+', ${object} '+verb,method,3)
# Coordinated operations retain their shared object and the finite clause's time/negation.
for first,converb,second in [
 ('incelenmiş','incelenerek','kategorize edilmiştir'),
 ('incelenmiş','incelenerek','sınıflandırılmıştır'),
 ('incelenmiş','incelenerek','kategorilere ayrılmıştır'),
 ('toplanmış','toplanarak','analiz edilmiştir'),
 ('toplanmış','toplanarak','değerlendirilmiştir'),
 ('analiz edilmiş','analiz edilerek','yorumlanmıştır'),
 ('değerlendirilmiş','değerlendirilerek','raporlanmıştır')]:
 add(r'\b'+obj+' '+first+' ve '+second+r'\b','${object} '+converb+' '+second,first+' ve')
# Study-design frame. No added clinician, causal claim, temporal sequence or completion claim.
add(r'\b'+obj+r' retrospektif olarak incelenmiş ve kategorize edilmiştir\b',
    'retrospektif inceleme kapsamında ${object}, incelenmiş ve kategorilere ayrılmıştır','retrospektif olarak')
add(r'\b'+obj+r' retrospektif olarak incelenmiş ve kategorize edilmiştir\b',
    '${object} retrospektif olarak incelenerek kategorize edilmiştir','retrospektif olarak')
# A preceding editable separator permits inserting a reporting frame even when the
# first institution name is an immutable Word anchor. It is not a zero-length edit.
add(r'(?<gap> +)'+obj+r' retrospektif olarak incelenmiş ve kategorize edilmiştir\b',
    '${gap}Retrospektif inceleme kapsamında ${object}, incelenmiş ve kategorilere ayrılmıştır','retrospektif olarak')
for head,referent in [('notları','notlar'),('kayıtları','kayıtlar'),('formları','formlar'),
                      ('dosyaları','dosyalar'),('veriler','veriler'),('belgeleri','belgeler'),
                      ('yanıtları','yanıtlar'),('raporları','raporlar')]:
 add(r'\b(?<object>[^.!?;\r\n\a]{3,1000}?) '+head+r' retrospektif olarak incelenmiş ve kategorize edilmiştir\b',
     '${object} '+head+' üzerinde retrospektif inceleme yapılmış ve '+referent+' kategorilere ayrılmıştır','retrospektif olarak')
for study in ['bu çalışmada','bu araştırmada','bu incelemede']:
 for operation in ['veriler','bulgular','sonuçlar','yanıtlar']:
  for ending in ['analiz edilmiştir','incelenmiştir','karşılaştırılmıştır']:
   add(r'\b'+study+' '+operation+' '+ending+r'\b',operation+', '+study+' '+ending,study,3)
# Comparative result frames preserve the comparison and its significance polarity.
for polarity in ['', 'anlamlı ', 'anlamlı olmayan ']:
 add(r'\b(?<groups>[^.!?;\r\n\a]{3,220}?) arasında '+polarity+r'bir fark bulunmuştur\b',
     '${groups} arasında '+polarity+'bir farklılık olduğu görülmüştür','fark bulunmuştur')
# Clinical entities supplement user locks; these are exact term protections, not replacements.
terms=['konsültasyon','diş hekimliği','restoratif diş tedavisi','protetik diş tedavisi',
'endodonti','periodontoloji','ortodonti','pedodonti','ağız diş ve çene cerrahisi',
'oral diagnoz','radyoloji','anabilim dalı','dentin','pulpa','dental implant',
'kompozit rezin','kök kanal tedavisi','gingivit','periodontit','çürük',
'uzman','lisansüstü öğrenci','klinik araştırma','randomizasyon','plasebo',
'istatistiksel anlamlılık','güven aralığı','standart sapma','p değeri',
'odds oranı','risk oranı','insidans','prevalans','kohort','kontrol grubu',
'konsültasyon talep formu','hasta kaydı','hasta dosyası','diş hekimliği fakültesi']
(root/'src/AcademicParaphraser.Core/Data/sentence-structures.json').write_text(json.dumps(rules,ensure_ascii=False,indent=2)+'\n')
p=root/'src/AcademicParaphraser.Core/Data/lexicon.json';lex=json.loads(p.read_text(encoding='utf-8-sig'))
for term in terms:
 if not any(x['Lemma']==term for x in lex):lex.append(dict(Lemma=term,Pos='Noun',Technical=True,Domain='genel',Confidence=1,Synonyms=[],AcademicAlternatives=[]))
p.write_text(json.dumps(lex,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(dict(newSentenceFrames=len(rules),lexiconEntries=len(lex))))
