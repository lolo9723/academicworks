#!/usr/bin/env python3
"""Create an indexed, read-only lexical data asset from pinned CC BY-SA KeNet XML.
No third-party source code is used; the derived data retains its source license.
"""
import argparse,hashlib,json,sqlite3,urllib.request,xml.etree.ElementTree as ET
from pathlib import Path

def main():
 p=argparse.ArgumentParser();p.add_argument('--offline',action='store_true');args=p.parse_args()
 root=Path(__file__).resolve().parent.parent;pin=json.loads((root/'build/lexical-data.json').read_text())
 cache=root/'artifacts/lexical-download';cache.mkdir(parents=True,exist_ok=True);source=cache/pin['dataFile']
 if not source.exists():
  if args.offline:raise FileNotFoundError('Pinned KeNet source is not cached')
  with urllib.request.urlopen(pin['url'],timeout=60) as response,source.open('wb') as destination:
   total=0
   while True:
    block=response.read(1024*1024)
    if not block:break
    total+=len(block)
    if total>100000000:raise ValueError('Lexical asset exceeds size bound')
    destination.write(block)
 if hashlib.sha256(source.read_bytes()).hexdigest()!=pin['sha256']:raise ValueError('KeNet SHA256 mismatch')
 out=root/'artifacts/lexical-data';out.mkdir(parents=True,exist_ok=True);target=out/'kenet.sqlite';temporary=out/'kenet.tmp.sqlite'
 temporary.unlink(missing_ok=True)
 db=sqlite3.connect(temporary)
 db.executescript('PRAGMA journal_mode=OFF; PRAGMA synchronous=OFF; CREATE TABLE senses(id TEXT PRIMARY KEY,pos TEXT NOT NULL,definition TEXT NOT NULL,example TEXT NOT NULL); CREATE TABLE members(lemma TEXT NOT NULL,sense_id TEXT NOT NULL); CREATE TABLE metadata(key TEXT PRIMARY KEY,value TEXT NOT NULL);')
 normalize=lambda s:s.translate(str.maketrans({'I':'ı','İ':'i'})).lower()
 count=0
 for event,synset in ET.iterparse(source,events=('end',)):
  if synset.tag!='SYNSET':continue
  identifier=synset.findtext('ID');pos=synset.findtext('POS') or ''
  definition=synset.findtext('DEF') or '';example=synset.findtext('EXAMPLE') or ''
  if identifier:
   db.execute('INSERT INTO senses VALUES(?,?,?,?)',(identifier,pos,definition,example))
   literals=synset.findall('LITERAL')+synset.findall('SYNONYM/LITERAL')
   for literal in literals:
    name=(literal.text or '').strip()
    if name:db.execute('INSERT INTO members VALUES(?,?)',(normalize(name),identifier))
   count+=1
  synset.clear()
 db.executescript('CREATE INDEX member_lemma ON members(lemma); CREATE INDEX member_sense ON members(sense_id);')
 stats={'synsets':count,'members':db.execute('SELECT count(*) FROM members').fetchone()[0],'lemmas':db.execute('SELECT count(DISTINCT lemma) FROM members').fetchone()[0]}
 if stats['synsets']<60000 or stats['lemmas']<60000:raise ValueError('KeNet XML parser did not import the expected real data')
 for k,v in {**pin,**stats,'formatVersion':1}.items():db.execute('INSERT INTO metadata VALUES(?,?)',(k,str(v)))
 db.commit();db.execute('VACUUM');db.close();temporary.replace(target)
 evidence={**pin,**stats,'bytes':target.stat().st_size,'sqliteSha256':hashlib.sha256(target.read_bytes()).hexdigest()}
 (out/'lexical-data-verification.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2)+'\n')
 notice=f"KeNet / Turkish WordNet — {pin['attribution']}\nSource: {pin['url']}\nSource SHA256: {pin['sha256']}\nLicense: CC BY-SA 4.0 — https://creativecommons.org/licenses/by-sa/4.0/\nLicense statement: {pin['licenseStatement']}\nChanges: XML sense/member records were normalized and indexed into SQLite. No source code from the upstream repository was incorporated. This derived lexical database is CC BY-SA 4.0.\n"
 (out/'KENET-NOTICE.txt').write_text(notice,encoding='utf-8-sig')
 print(json.dumps(evidence,ensure_ascii=False))

if __name__=='__main__':main()
