#!/usr/bin/env python3
"""Compile the real VSTO C# sources against Microsoft's pinned SDK.

This is a source/API check, not a VSTO manifest build or a live Word test.
The official SDK stays in the developer cache and is never redistributed.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import urllib.request
import xml.etree.ElementTree as ET
import zipfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--configuration", choices=["Debug", "Release"], default="Release")
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent
    pin = json.loads((root / "build/vsto-sdk.json").read_text(encoding="utf-8"))
    cache = root / "artifacts/vsto-sdk"
    cache.mkdir(parents=True, exist_ok=True)
    archive = cache / pin["fileName"]
    if not archive.exists():
        with urllib.request.urlopen(pin["url"], timeout=60) as response:
            data = response.read(20_000_001)
        if len(data) > 20_000_000:
            raise ValueError("SDK archive exceeds the download limit")
        if hashlib.sha256(data).hexdigest() != pin["sha256"]:
            raise ValueError("Official SDK SHA256 mismatch")
        archive.write_bytes(data)
    if hashlib.sha256(archive.read_bytes()).hexdigest() != pin["sha256"]:
        raise ValueError("Cached SDK SHA256 mismatch")
    extracted = cache / "extracted"
    with zipfile.ZipFile(archive) as package:
        for item in package.infolist():
            dest = (extracted / item.filename).resolve()
            if not dest.is_relative_to(extracted.resolve()):
                raise ValueError("Unsafe SDK archive path")
        package.extractall(extracted)
    sdk = extracted / pin["referencePath"]
    subprocess.run([args.dotnet, "build", "src/AcademicParaphraser.WordHost/AcademicParaphraser.WordHost.csproj",
                    "-c", args.configuration, "-warnaserror"], cwd=root, check=True)
    packages = Path(os.environ.get("NUGET_PACKAGES", str(Path.home() / ".nuget/packages")))
    framework = packages / "microsoft.netframework.referenceassemblies.net48/1.0.3/build/.NETFramework/v4.8"
    interop = packages / "microsoft.office.interop.word/15.0.4797.1004"
    word_dll = next(p for p in interop.rglob("*.dll") if p.name.lower() == "microsoft.office.interop.word.dll")
    installed = subprocess.check_output([args.dotnet, "--list-sdks"], text=True)
    sdks = []
    for line in installed.splitlines():
        match = re.fullmatch(r"(\S+) \[(.+)\]", line)
        if match:
            sdks.append((tuple(int(n) for n in re.findall(r"\d+", match[1])), Path(match[2]) / match[1]))
    compiler = max(sdks)[1] / "Roslyn/bincore/csc.dll"
    host = root / "src/AcademicParaphraser.WordHost/bin" / args.configuration / "net48"
    project_path = root / "src/AcademicParaphraser.WordAddin/AcademicParaphraser.WordAddin.csproj"
    project = ET.parse(project_path)
    ns = {"m": "http://schemas.microsoft.com/developer/msbuild/2003"}
    out = root / "artifacts/sdk-compile-check" / args.configuration
    out.mkdir(parents=True, exist_ok=True)
    cmd = [args.dotnet, str(compiler), "/noconfig", "/nostdlib+", "/target:library", "/langversion:latest",
           "/nullable:enable", "/warnaserror+", "/deterministic+", "/out:" + str(out / "AcademicParaphraser.WordAddin.dll")]
    # Classic MSBuild implicitly references the Framework core library.
    cmd.append("/reference:" + str(framework / "mscorlib.dll"))
    # Use exactly the project's declared references, including its interop embedding policy.
    for item in project.findall("m:ItemGroup/m:Reference", ns):
        name = item.attrib["Include"].split(",")[0] + ".dll"
        candidates = [framework / name, framework / "Facades" / name, sdk / name]
        if name.lower() == "office.dll":
            candidates = [sdk / "Office15/Office.dll"]
        elif name.lower() == "microsoft.office.interop.word.dll":
            candidates = [word_dll]
        reference = next((p for p in candidates if p.exists()), None)
        if reference is None:
            raise FileNotFoundError("Declared reference not found: " + name)
        embed = item.find("m:EmbedInteropTypes", ns)
        cmd.append(("/link:" if embed is not None and embed.text == "true" else "/reference:") + str(reference))
    for item in project.findall("m:ItemGroup/m:ProjectReference", ns):
        name = Path(item.attrib["Include"].replace("\\", "/")).stem + ".dll"
        cmd.append("/reference:" + str(host / name))
    for item in project.findall("m:ItemGroup/m:Compile", ns):
        cmd.append(str(project_path.parent / item.attrib["Include"].replace("\\", "/")))
    for item in project.findall("m:ItemGroup/m:EmbeddedResource", ns):
        path = project_path.parent / item.attrib["Include"].replace("\\", "/")
        cmd.append("/resource:" + str(path) + ",AcademicParaphraser.WordAddin." + path.name)
    result = subprocess.run(cmd, cwd=root, text=True, capture_output=True)
    evidence = {"sdkVersion": pin["version"], "sdkSha256": pin["sha256"], "configuration": args.configuration,
                "exitCode": result.returncode, "scope": "C# source/API compilation only; no VSTO manifests, installer or live Word",
                "compilerOutput": result.stdout + result.stderr}
    (out / "evidence.json").write_text(json.dumps(evidence, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(result.stdout + result.stderr)
    if result.returncode:
        raise SystemExit(result.returncode)
    print("PASS: real WordAddin sources and declared references; Windows deployment remains unverified.")


if __name__ == "__main__":
    main()
