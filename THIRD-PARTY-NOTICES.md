# Üçüncü taraf bileşenler

Bu kaynak teslimi ve Windows derleme süreci aşağıdaki bileşenleri kullanır. Sürüm/bağımlılıkların tam listesi NuGet restore raporları ve Maven dependency tree ile üretilebilir. Bileşenlerin kendi lisans/copyright dosyaları saklanmalıdır; bu liste onların yerine geçmez.

| Bileşen | Sürüm / lisans | Kaynak |
| --- | --- | --- |
| Zemberek NLP | 0.17.1 — Apache-2.0 | https://github.com/ahmetaa/zemberek-nlp |
| Gson | 2.11.0 — Apache-2.0 | https://github.com/google/gson |
| Guava / Java transitif bağımlılıklar | Guava 33.4.8-jre, Protobuf 3.25.8, Caffeine 2.9.3 / Maven ağacında — Apache-2.0 / ilgili bileşen lisansı | https://github.com/google/guava |
| Newtonsoft.Json | 13.0.3 — MIT | https://github.com/JamesNK/Newtonsoft.Json |
| Microsoft.Data.Sqlite | 8.0.11 — MIT | https://github.com/dotnet/efcore |
| SQLitePCLRaw | 2.1.13 — Apache-2.0 | https://github.com/ericsink/SQLitePCL.raw |
| SQLite | SQLitePCLRaw içinde — public domain | https://sqlite.org/copyright.html |
| System.Security.Cryptography.ProtectedData / .NET bağımlılıkları | 8.0.0 / restore sürümleri — MIT | https://github.com/dotnet/runtime |
| Html Agility Pack | 1.11.71 — MIT | https://github.com/zzzprojects/html-agility-pack |
| Office Interop / .NET Framework Reference Assemblies | NuGet/Visual Studio sürümleri — Microsoft lisansları | https://www.nuget.org/packages/Microsoft.Office.Interop.Word |
| Eclipse Temurin/OpenJDK JRE | build/java-runtime.json — GPL-2.0 with Classpath Exception ve runtime legal içeriği | https://github.com/adoptium/temurin17-binaries |
| xUnit | 2.9.2 / runner 2.8.2 — Apache-2.0 | https://github.com/xunit/xunit |
| Microsoft.NET.Test.Sdk | 17.11.1 — MIT | https://github.com/microsoft/vstest |
| Inno Setup | 6.3+ — Inno Setup license, installer derleme aracı | https://jrsoftware.org/isinfo.php |

Windows JRE'nin `legal` dizini paketleme sırasında korunur. Kaynak kodu ve ilgili OpenJDK release karşılıkları Temurin sürüm deposunda bulunur. Ürünün installer tarifi JDK'yı son kullanıcıya yükletmez; bundled JRE'yi kullanır. Java runtime'ı bu kaynak ZIP'inde bulunmaz, Windows paketleme betiği resmi kaynaktan indirip SHA256 doğrular.

Wiktionary/Vikisözlük sözlük içeriği, kaynak sayfasının CC BY-SA koşulları ve katkıcı atfına tabidir. Uygulama sonuçta sayfa bağlantısı/lisans yönlendirmesi verir. Wikipedia/Wikimedia veya sözlük katkıcıları bu projeyi destekliyor gibi gösterilmez. Çevrimiçi içerik önceden kaynak paketine kopyalanmamıştır.

Zemberek ve Java transitif JAR lisans/NOTICE dosyalarının kopyaları `licenses/java` altında, NuGet bileşenlerinin nupkg içindeki lisans metinleri `licenses/nuget` altında bulunabilir. Windows VSTO/Office kurulumlarının Microsoft EULA'sı son kullanıcının kendi kurulumuna bağlıdır; Word bu projeyle dağıtılmaz.

## KeNet lexical data

Turkish WordNet / KeNet, Starlang Software and contributors. Upstream data is separately licensed CC BY-SA 4.0; upstream GPL source code is not incorporated. The pinned native XML is normalized into `data/kenet.sqlite`, which retains CC BY-SA 4.0. Attribution, exact source URL/commit and changes are in `data/KENET-NOTICE.txt`; build pin is `build/lexical-data.json`. License: https://creativecommons.org/licenses/by-sa/4.0/. Source: https://github.com/StarlangSoftware/TurkishWordNet.

Wikidata term-information fallback uses CC0 structured data via the public MediaWiki interface (https://www.wikidata.org/wiki/Wikidata:Licensing). Only an exact Turkish term/alias match is shown, with the entity source URL; fuzzy or other-language matches are rejected. These descriptions are not used as synonyms or rewriting rules.

## Yerel yeniden yazım

Varsayılan Qwen3-4B-Instruct-2507 (Qwen, Apache-2.0), Unsloth topluluk Q4_K_M GGUF dönüşümü. Model deposu: https://huggingface.co/unsloth/Qwen3-4B-Instruct-2507-GGUF ; sabit revision a06e946bb6b655725eafa393f4a9745d460374c9. Deneysel Qwen3.5 4B/9B profillerinin sabit kaynakları MODEL-SOURCES.md içinde yer alır. SHA256 ve boyut `LocalModelStore.cs` içinde sabittir. Model uygulama EXE'sine dahil değildir; kullanıcı düğmeyle indirir. Lisans ve sabit model kartı bağlantısı `licenses/local-model` dizinindedir. Nicemleme Qwen'in resmi GGUF sürümü diye sunulmaz.

llama.cpp b11460, MIT, CPU x64 binary: https://github.com/ggml-org/llama.cpp/tree/b11460 . Sabit Windows arşivi ve SHA256 `build/local-runtime.json` içindedir. MIT ve LLVM/OpenMP lisans dosyaları pakette korunur. Microsoft Word ve uygulama çalışırken yerel sunucu yalnızca loopback adresinde, geçici yerel erişim anahtarıyla açılır; belgeyi buluta göndermez. Bu anahtar kullanıcının satın aldığı bir API anahtarı değildir.
