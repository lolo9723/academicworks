#!/usr/bin/env python3
"""Prepare a local, pinned NLI classifier. Never upload document text."""
import hashlib,json,os,pathlib,shutil,urllib.request,zipfile
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  while b:=f.read(1048576):h.update(b)
 return h.hexdigest()
root=pathlib.Path(__file__).resolve().parent.parent
pin=json.loads((root/'build/semantic-backend.json').read_text());cache=root/'artifacts/semantic-research';cache.mkdir(exist_ok=True)
destination=root/'runtime/semantic';destination.mkdir(parents=True,exist_ok=True)
for asset in pin['assets']:
 p=cache/pathlib.Path(asset['file']).name
 if not p.exists():
  partial=p.with_suffix(p.suffix+'.partial')
  with urllib.request.urlopen('https://huggingface.co/'+pin['model']+'/resolve/'+pin['revision']+'/'+asset['file'],timeout=90) as src,partial.open('wb') as dst:shutil.copyfileobj(src,dst)
  partial.replace(p)
 if p.stat().st_size!=asset['bytes'] or sha(p)!=asset['sha256']:raise ValueError('Semantic asset integrity failed: '+p.name)
 shutil.copyfile(p,destination/p.name)
shutil.copyfile(root/'semantic/target/local-semantic-1.0.0.jar',destination/'local-semantic-1.0.0.jar')
libraries=destination/'lib';libraries.mkdir(exist_ok=True);repacked=[]
# Keep original notices/classes, and only the current platform's CPU native
# binaries. The patched Apache-2.0 DJL loader does not load GCC runtime DLLs.
platform_ort='win-x64' if os.name=='nt' else 'linux-x64';platform_djl='win-x86_64' if os.name=='nt' else 'linux-x86_64'
for original in sorted((root/'semantic/target/lib').glob('*.jar')):
 target=libraries/original.name
 if original.name.startswith(('onnxruntime-','tokenizers-')):
  with zipfile.ZipFile(original) as src,zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED) as dst:
   for info in src.infolist():
    name=info.filename
    if '/native/' in name and not name.endswith('/') and '/'+platform_ort+'/' not in name:continue
    if name.startswith('native/lib/') and not name.endswith('/') and name!='native/lib/tokenizers.properties' and '/'+platform_djl+'/cpu/' not in name:continue
    if pathlib.PurePosixPath(name).name in {'libwinpthread-1.dll','libgcc_s_seh-1.dll','libstdc++-6.dll'}:continue
    dst.writestr(info,src.read(name))
  repacked.append({'file':original.name,'originalSha256':sha(original),'runtimeSha256':sha(target),'patch':'current CPU platform only; unused GCC native binaries excluded; notices retained'})
 else:shutil.copyfile(original,target)
native=destination/'native';native.mkdir(exist_ok=True)
for name in ('onnxruntime-'+pin['onnxRuntime']+'.jar','tokenizers-'+pin['djlTokenizers']+'.jar'):
 with zipfile.ZipFile(libraries/name) as z:
  for info in z.infolist():
   if info.filename.endswith(('.dll','.so')):(native/pathlib.Path(info.filename).name).write_bytes(z.read(info))
report=dict(pin,runtimeJarRepacking=repacked,generativeModel=False,userTextUsedForTraining=False,modelWasFineTunedHere=False)
(destination/'semantic-model-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
(cache/'semantic-model-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'modelBytes':pin['assets'][0]['bytes'],'runtimeLibrariesBytes':sum(p.stat().st_size for p in libraries.iterdir()),'destination':str(destination)}))
