#!/usr/bin/env python3
"""Train compact linguistic parsers on pinned public data. No user text or LLM.
Models and held-out reports retain CC BY-SA attribution; UDPipe remains unmodified.
"""
import argparse,concurrent.futures,hashlib,json,os,pathlib,re,shutil,subprocess,time,urllib.request,zipfile

def digest(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  while b:=f.read(1024*1024):h.update(b)
 return h.hexdigest()
def fetch(p,url,expected,size):
 if not p.exists():
  p.parent.mkdir(parents=True,exist_ok=True)
  part=p.with_suffix(p.suffix+'.partial')
  with urllib.request.urlopen(url,timeout=90) as response,part.open('wb') as dest:shutil.copyfileobj(response,dest)
  part.replace(p)
 if p.stat().st_size!=size or digest(p)!=expected:raise ValueError('Pinned file integrity failed: '+p.name)
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--jobs',type=int,default=2);parser.add_argument('--reuse-training',action='store_true');args=parser.parse_args()
 root=pathlib.Path(__file__).resolve().parent.parent;pin=json.loads((root/'build/dependency-backend.json').read_text(encoding='utf-8-sig'))
 cache=root/'artifacts/research-udpipe';archive=cache/'udpipe-1.3.0-bin.zip';fetch(archive,pin['runtime']['url'],pin['runtime']['sha256'],pin['runtime']['bytes'])
 extracted=cache/'udpipe-1.3.0-bin'
 if not extracted.exists():
  with zipfile.ZipFile(archive) as z:
   for entry in z.infolist():
    if not (cache/entry.filename).resolve().is_relative_to(cache.resolve()):raise ValueError('Unsafe runtime path')
   z.extractall(cache)
 platform='bin-win64' if os.name=='nt' else 'bin-linux64';exe=extracted/platform/('udpipe.exe' if os.name=='nt' else 'udpipe');exe.chmod(0o755)
 training=root/'artifacts/dependency-training'
 for asset in pin['inputs']:fetch(training/asset['path'],asset['url'],asset['sha256'],asset['bytes'])
 models=root/'artifacts/dependency-models';models.mkdir(exist_ok=True);destination=root/'runtime/udpipe';destination.mkdir(parents=True,exist_ok=True)
 def train(spec):
  language,repo,prefix=spec;model=models/(language+'-boun-ewt.udpipe');data=training/repo;started=time.monotonic()
  if not args.reuse_training or not model.exists() or model.stat().st_size==0:
   command=[str(exe),'--train','--heldout='+str(data/(prefix+'-ud-dev.conllu'))]+['--'+k+'='+v for k,v in pin['parameters'].items()]+[str(model),str(data/(prefix+'-ud-train.conllu'))]
   with (models/(language+'-training.log')).open('w',encoding='utf-8') as log:subprocess.run(command,stdout=log,stderr=subprocess.STDOUT,check=True,timeout=5400)
  accuracy=models/(language+'-heldout-test.log')
  with accuracy.open('w',encoding='utf-8') as log:subprocess.run([str(exe),'--tag','--parse','--accuracy',str(model),str(data/(prefix+'-ud-test.conllu'))],stdout=log,stderr=subprocess.STDOUT,check=True,timeout=300)
  shutil.copyfile(model,destination/model.name)
  return language,{'file':model.name,'sha256':digest(model),'bytes':model.stat().st_size,'ownTraining':True,'data':repo,'trainingParameters':pin['parameters'],'elapsedSeconds':time.monotonic()-started,'testUsesGoldTokenization':True,'testReport':accuracy.read_text(encoding='utf-8')}
 specs=[('tr','UD_Turkish-BOUN','tr_boun'),('en','UD_English-EWT','en_ewt')]
 with concurrent.futures.ThreadPoolExecutor(max_workers=max(1,min(2,args.jobs))) as pool:results=dict(pool.map(train,specs))
 runtime=destination/('udpipe.exe' if os.name=='nt' else 'udpipe');shutil.copyfile(exe,runtime);runtime.chmod(0o755)
 report={'runtimeVersion':'1.3.0','runtimeSha256':digest(runtime),'runtimeArchiveSha256':pin['runtime']['sha256'],'models':results,'inputs':pin['inputs'],'generativeModelUsed':False,'userTextUsedForTraining':False,'treebankVersion':pin['treebankVersion']}
 (destination/'dependency-model-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 (models/'dependency-model-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 shutil.copyfile(extracted/'LICENSE',destination/'UDPipe-MPL-2.0.txt')
 for repo in ['UD_Turkish-BOUN','UD_English-EWT']:
  shutil.copyfile(training/repo/'LICENSE.txt',destination/(repo+'-LICENSE.txt'));shutil.copyfile(training/repo/'README.md',destination/(repo+'-README.md'))
 (destination/'MODEL-SOURCES.txt').write_text('These compact parsers were trained by this project using UD 2.17 Turkish-BOUN and English-EWT (CC BY-SA 4.0). Derived models retain CC BY-SA 4.0. Reproduce with build/Prepare-DependencyBackend.py. Unmodified UDPipe 1.3.0: https://github.com/ufal/udpipe/tree/v1.3.0 (MPL-2.0). Source, parameters and individual data hashes are in build/dependency-backend.json. No user document was used in training.\n',encoding='utf-8')
 print(json.dumps({'models':{k:{'bytes':v['bytes'],'sha256':v['sha256']} for k,v in results.items()},'destination':str(destination)},ensure_ascii=False))
if __name__=='__main__':main()
