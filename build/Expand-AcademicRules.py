#!/usr/bin/env python3
"""Build the reviewed finite tense tables and academic sentence templates.
Rule count is not a claim of independent syntactic families or measured accuracy.
"""
from pathlib import Path
import json,re
root=Path(__file__).resolve().parent.parent;p=root/'src/AcademicParaphraser.Core/Data/rules.json'
templates=json.loads((p.parent/'rule-templates.json').read_text(encoding='utf-8-sig'))
rules=[];index=0
context=templates['context']
def add(pattern,target,family,strength=2,confidence=.95,academic=False):
 global index
 hint=''
 if 'en etkili' in pattern:hint='en etkili'
 elif 'mümkün kıldığı için' in pattern:hint='mümkün kıldığı için'
 elif 'amacı' in pattern:hint='amacı'
 elif not any(x in pattern.replace(r'\b','') for x in ['[','(','?','+','{']):hint=pattern.replace(r'\b','').replace('\\','')
 index+=1;rules.append(dict(Id=f'extended-{index:04}',Pattern=pattern,Target=target,Family=family,Strength=strength,Confidence=confidence,PosConstraint='',ContextPattern=context if academic else '',RequiredText=hint,RequiredMorphemes=[],ForbiddenMorphemes=[],Domain='genel'))
# Explicit inflection tables preserve voice, tense and negation in these registered predicates.
e=templates['e']
i=templates['i']
u=templates['u']
front_round=templates['front_round']
a=templates['a']
predicates=templates['predicates']
for source,ss,target,ts in predicates:
 for x,y in zip(ss,ts):add(r'\b'+re.escape(source+x)+r'\b',target+y,'yüklem',2,.95,True)
# Stable method frames are finite; a captured topic is never guessed or invented.
methods=templates['methods']
for work in ['çalışma','araştırma']:
 for demonstrative in ['bu','mevcut','sunulan']:
  for method in methods:
   # Same operation and tense; only the reporting frame changes.
   for past,targetpast in [('kullanılmıştır','kullanılarak yürütülmüştür'),('kullanılmaktadır','kullanılarak yürütülmektedir')]:
    add(r'\b'+f'{demonstrative} {work}da {method} {past}'.replace('araştırmada','araştırmada')+r'\b',f'{demonstrative} {work} {method} {targetpast}','yöntem',2,.96)
   for operation,replacement in [('yapılmıştır','gerçekleştirilmiştir'),('yapılmaktadır','gerçekleştirilmektedir'),('uygulanmıştır','uygulanmıştır'),('uygulanmaktadır','uygulanmaktadır')]:
    add(r'\b'+re.escape(f'{demonstrative} {work}da {method} {operation}')+r'\b',f'{demonstrative} {work} kapsamında {method} {replacement}','yöntem',2,.96)
for work in ['çalışma','araştırma']:
 for suffix,nominal in [('manın','ma'),('manın','ma')][:1]:
  for ending,targetending in [('maktır','mayı'),('mektir','meyi')]:
   add(r'\bbu '+work+r'nın amacı,? (?<goal>[^.!?;\r\n\a]{3,220})'+ending+r'\b','bu '+work+', ${goal}'+targetending+' amaçlamaktadır','yüklem merkezli',2,.97)
 for method in ['anket','görüşme','gözlem','doküman incelemesi','içerik analizi']:
  for source,target in [('toplanmıştır','yararlanılmıştır'),('toplanmaktadır','yararlanılmaktadır')]:
   add(r'\bbu '+work+r'da veriler '+re.escape(method)+r' (?:yoluyla|aracılığıyla) '+source+r'\b','bu '+work+'da verilerin toplanmasında '+method+' yönteminden '+target,'isim-fiil',2,.96)
   add(r'\bveriler bu '+work+r'da '+re.escape(method)+r' (?:yoluyla|aracılığıyla) '+source+r'\b','bu '+work+'da verilerin toplanmasında '+method+' yönteminden '+target,'güvenli sıralama',3,.96)
# General purpose/method clause frames, including preservation of technical-term anchors.
for singular,plural in [('yol','yollar'),('yöntem','yöntemler'),('araç','araçlar'),('teknik','teknikler')]:
 # The two plural inflection classes are set explicitly below.
 # Subject-first alternative remains available when a protected first-word anchor prevents moving it.
 pluralposs='yolları' if singular=='yol' else 'yöntemleri' if singular=='yöntem' else 'araçları' if singular=='araç' else 'teknikleri'
 sourceplural=plural+'ından' if singular in ['yol','araç'] else plural+'inden'
 add(r'\b(?<tool>[^,.;!?\r\n\a]{5,85}), (?<goal>[^,.;!?\r\n\a]{5,160}) en etkili '+sourceplural+r' biridir\b','${goal} en etkili '+pluralposs+' arasında ${lower:tool} yer alır','güvenli sıralama',3,.96)
 add(r'\b(?<tool>[^,.;!?\r\n\a]{5,85}), (?<goal>[^,.;!?\r\n\a]{5,160}) en etkili '+sourceplural+r' biridir\b','${tool}, ${goal} en etkili '+pluralposs+' arasında yer almaktadır','yüklem merkezli',2,.96)
for infinitive,acc,dat in [('ma','yı','ya'),('me','yi','ye')]:
 add(r'\b(?<goal>[^.!?;\r\n\a]{3,180}'+infinitive+')'+acc+r' mümkün kıldığı için\b','${goal}'+dat+' olanak sağladığından','neden-sonuç',2,.96)
# Academic reporting alternatives with no new actor, fact, causality or tense.
phrases=templates['phrases']
for source,target,family in phrases:add(r'\b'+re.escape(source)+r'\b',target,family,2,.94)
# Avoid presenting an uncompleted study as completed: purpose frames always keep present intention.
ids={(r['Pattern'],r['Target']):1 for r in json.loads(p.read_text(encoding='utf-8-sig'))};unique=[]
for r in rules:
 key=(r['Pattern'],r['Target'])
 if key not in ids:unique.append(r);ids[key]=1
out=root/'artifacts/rules';out.mkdir(parents=True,exist_ok=True)
import uuid
working=out/('rules.'+uuid.uuid4().hex+'.tmp')
working.write_text(json.dumps(unique,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
working.replace(out/'rules.extended.json')
print(json.dumps({'totalRules':len(unique)+61,'newRules':len(unique),'templateFamilies':len({r['Family'] for r in unique})}))
