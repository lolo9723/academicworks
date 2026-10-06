# Geliştirici belgesi

## Derleme sınırları

VSTO, Windows'taki Visual Studio OfficeTools MSBuild hedeflerini gerektirir. .NET SDK'nın Linux'ta .NET Framework reference assemblies ile WordHost'u derlemesi VSTO başlangıç projesi, native COM yükleme veya UI testi değildir. `AcademicParaphraser.sln` tam Windows solution'ıdır; `AcademicParaphraser.Engine.sln` bu host bağımlılığını ayırır. Windows betiği VSTO manifestleri ve Windows native dosyaları olmadan installer üretmeyi reddeder.

Kütüphane/proje sürümü 1.0.2'dir; bu numara Windows/Word kabul sertifikası anlamına gelmez. Ürün sürümü değişiminde `Directory.Build.props`, WordAddin AssemblyInfo, VSTO ApplicationVersion, Installer AppVersion/çıktı adı ve workflow artefakt adları birlikte güncellenir. NLP sidecar bağımsız sürümlenir; bu sürümde Maven/JAR ve Java çağrı yolları hâlâ `turkish-nlp-1.0.0.jar` kullanır.

## Veri ve bağımlılık akışı

`AddinController` Word UI adaptörü ile engine/repository/NLP/provider bağımlılıklarını bağlayan composition root'tur. Core Word veya WinForms referansı almaz. Engine `IRuleCatalog`, `ITurkishNlp` ve isteğe bağlı `ILexicalSource`, dictionary servisi `IDictionaryProvider`, history repository `ITextProtector` arayüzleri kullanır. Ana ürün DPAPI kullanır; cross-platform testlerde rastgele anahtarlı AES-CBC/HMAC sağlayıcısı gerçek şifreleme kontrolü için kullanılır.

1. Word UI thread'inde seçim, görünür metin ve OOXML snapshot alınır.
2. Korumalar regex, kullanıcı terimleri, OOXML ve native alanlar üzerinden oluşturulur.
3. Engine arka planda, paragraf/hücre bazında gerçek morfoloji analizi alır.
4. Domain/strength/confidence/POS/morfem/bağlam kısıtlı kurallar ve çekilebilir sözlük alternatifleri değerlendirilir.
5. Çakışan edit aralıkları birleştirilmez; deterministik farklı adaylar seçilir.
6. UI thread'inde run koruma filtresi tekrar uygulanır ve önizleme gösterilir.
7. Kullanıcı uygulayınca snapshot'ın değişmediği kontrol edilir, Word.Find ile edit konumları doğrulanır ve yalnızca küçük native aralıklar ters sırada yazılır.
8. URL hedefleri karşılaştırılır; hata durumunda aynı custom Undo kaydı geri alınır. Başarılı işlemin geçmişi DPAPI blob olarak saklanır.

Word nesneleri `Task.Run` içine taşınmaz. Arka plandaki lambda snapshot'ın salt metin/koruma verilerini kullanır; COM alanlarını çağırmaz. Controller kapanışta iptal eder, sonradan dönen async sonucun paneli güncellemesini engeller. Controller'daki snapshot COM referansları UI thread'inde bırakılır.

## OOXML ve native Word

`OoxmlRunMap` güvenli XML reader kullanır; external entities/DTD engellidir. Varsayılan ana belge parçasını, diğer Word story'leri için footnotes/endnotes/header/footer parçasını okuyabilir. Aynı biçim anahtarına sahip birden fazla run içindeki kalıp değerlendirilebilir. Run'ların hyperlink/field konteyneri aynı olmalıdır. Farklı fontlar arasında veya link dışı/içi arasında tek edit yapılamaz.

OOXML yazma yöntemi test edilebilir saf bileşendir. Canlı host paragrafı yeniden OOXML import ederek yazmaz; yalnızca doğrulanmış aralıkların `Range.Text` değerlerini günceller, saklanan Font'u tekrar uygular. Bu, tüm seçimi düz metin olarak yazmakla aynı değildir. Paragraf sonları, alan kodları ve hücre sonları yazma aralığına alınmaz.

Snapshot eşleme tekil değilse işlem durur. Nesne işaretleri, mevcut revisions ve karmaşık story export'ları nedeniyle bütün seçimi dönüştürmek mümkün olmayabilir. Bu durumda yeniden yazma ile kör düzeltme yapılmaz. Alan kodları her zaman korunur; yalnızca kullanıcı link korumasını kapatırsa HYPERLINK result metni düzenlenebilir. Citation/REF/ADDIN fields düzenlenmez. Yer işaretleri korunması için kapsadıkları metin kilitlenir.

Native Word davranışı özellikle Undo/Track Changes, field result yazma, farklı bitness ve story export'larında Linux testleriyle kanıtlanamaz. `Windows-Acceptance.md` kontrolleri zorunlu yayın kriteridir.

## Kural ekleme

Son kullanıcı Sözlük > Dönüşüm kuralları ekranından kayıt ekler. Geliştirici temel veri için `Core/Data/rules.json`, genişletilmiş çekim/kalıp tabloları için `Core/Data/rule-templates.json` dosyasını değiştirir. Core derleme hedefi `build/Expand-AcademicRules.py` ile 61 temel kurala ek olarak 1.191 kural üretir; çıktısı `artifacts/rules/rules.extended.json` gömülü kaynak olarak derlenir. Üretimde Python çalıştırılmaz. Varsayılan kayıtlar ilk kullanımda SQLite'a eklenir. Mevcut DB'de aynı ID değiştirilmez; yayımlanmış bir seed değişikliği için migration gerekir.

Örnek kayıt:

```json
{
  "Id": "personal-yontem-01",
  "Pattern": "\\bveriler çözümlenmiştir\\b",
  "Target": "veriler analiz edilmiştir",
  "Family": "yöntem",
  "Strength": 1,
  "Confidence": 0.95,
  "PosConstraint": "Verb",
  "ContextPattern": "araştırma|çalışma",
  "RequiredMorphemes": [],
  "ForbiddenMorphemes": ["Neg"],
  "Domain": "genel"
}
```

Pattern ve ContextPattern Türkçe küçük harfe dönüştürülmüş kaynak üzerinde .NET regex olarak çalışır. Target .NET `Match.Result` yakalama biçimini destekler. İlk/bütün büyük harf biçimi geri uygulanır. ContextPattern yalnızca eşleşmenin bulunduğu paragraf/hücre bağlamında aranır. POS/morfem kısıtları eşleşmeye dahil token'lar üzerinden değerlendirilir; bu mekanizma tam dependency parser değildir.

Pattern boş olmamalı/boş metinle eşleşmemeli. ID, düzey, güven ve regex geçerliliği kaydederken/import ederken doğrulanır. Regex çalışma zaman aşımı 150 ms'dir. Düşük güvenli, olumsuzluk/kip/kişi/nesne sınırı değiştiren veya bağlamı belirsiz kurala yüksek güven etiketi vermeyin. İlgili anlam korunma ve olumsuz örnek regresyonunu ekleyin. Güven puanı elle verilmiş bir ön değerlendirmedir, model doğruluk oranı değildir.

## Sözlük ve morfolojik üretim

Lexicon alanları lemma, POS, synonyms, nearSynonyms, forbiddenReplacements, academicAlternatives, domain, confidence, examples, technical ve userDefined'dır. Arayüzde listeler noktalı virgül ile girilir. POS Zemberek adlarıyla aynı olmalı (`Noun`, `Verb`, `Adjective` gibi). Lemma ve hedef lemma Zemberek sözlüğünde mevcut olmalıdır.

Otomatik değişim yalnızca synonyms/academicAlternatives üzerinden yapılır. ForbiddenReplacements çıkartılır; NearSynonyms yalnızca bilgi olarak kalır. Teknik terimler koruma açıksa değiştirilmez. Yeni sözcüğün çekimi Zemberek WordGenerator üzerinden, kaynak ek zincirini taşıyarak üretilir. Birden fazla olası yüzey biçimi varsa sonuç null olur ve değişim atlanır. Kaynak anlamı bağlama göre farklı olan sözlük karşılıkları için bu katman tek başına yeterli güven kanıtı değildir.

## Java sidecar

`nlp/pom.xml` Java 17 bytecode ve Zemberek 0.17.1 kullanır. Maven shade, gerçek dil kaynaklarını içeren çalıştırılabilir JAR üretir. C# onu `java.exe -Dfile.encoding=UTF-8 -Xmx512m -jar ...` ile otomatik açar. Ağ dinleyicisi/port yoktur; yerel stdin/stdout JSON-lines protokolü kullanılır. Başlangıçta `ready`, her istekte bir JSON cevap vardır.

- `analyze`: metin → UTF-16 indeksli token, lemma, POS, proper, morphemes.
- `inflect`: kaynak yüzey + hedef lemma + POS → tek ve güvenli yüzey veya null.
- `shutdown`: süreç kapanır.

Startup 35 saniye, işlem 120 saniye sınırındadır. Kullanıcı iptalinde süreç durdurulur; sonraki istekte yeniden başlar. Java stderr belge metni loglarına aktarılmaz. NLP erişimi semaphore ile seri hâle getirilir. Zemberek'in yerel morfoloji disambiguation bileşeni üretken dil modeli değildir.

Windows runtime `build/java-runtime.json` ile sürüm/URL/SHA256 olarak sabitlenir. Yeni Java güvenlik sürümünde üç alanı resmi sürüm/özet kaynaklarından birlikte güncelleyin. Bundled JRE'nin legal dosyalarını installer payload'ında koruyun. Runtime kaybolursa anlaşılır kurulum onarma hatası gösterilir; sessiz synonym fallback yoktur.

## Yeni dictionary provider

Infrastructure/InternetDictionaryProviders altında `IDictionaryProvider` uygulayın. `Name` cache anahtarının bir parçasıdır; kalıcı ve tekil tutun. `LookupAsync` yalnızca çağrılan tek kelimeyi alır; HTTP HTTPS, timeout, iptal, User-Agent, yanıt boyutu sınırı, kaynak URL'si ve lisans atfı sağlamalıdır. Hatalar HttpRequestException/InvalidOperationException/JsonException olarak servis tarafından yalıtılır; kullanıcı iptali ile provider timeout'u ayrılır.

Yeni sağlayıcıyı composition root'taki listeye ekleyin. Belge metni sağlayıcıya verilmez. DictionaryService offline/internet ayarını provider çağrısından önce kontrol eder, 30 günlük cache'i okur ve isteğe bağlı kaydeder. Bir provider diğerlerinden bağımsızdır. Sağlayıcının döndürdüğü açıklamalar kendiliğinden güvenli parafraz kuralı olmaz.

Mevcut sağlayıcılar sitenin HTML sayfasını scraping yapmaz; açık MediaWiki parse arayüzünün JSON içindeki sözlük içeriğini ayrıştırır. API anahtarı yoktur. Kaynak içeriği farklı lisanslı alt unsurlar barındırabilir; sonuçta kaynak URL ve CC BY-SA yönlendirmesi korunur. Çalışma sırasında geniş çaplı sözlük indirmesi uygulanmaz. Derleme sırasında ayrı ve SHA256 ile sabitlenmiş KeNet verisi hazırlanır; belge metni bu hazırlığa karışmaz.

## SQLite, migration ve yedekleme

Uygulama verisi `%LOCALAPPDATA%\AkademikParafraz\academic.sqlite` içindedir. Her işlem parametreli SQL ve kilit kullanır. History önce DPAPI ile şifrelenir. Migration 1 mevcut kullanıcı kayıtlarının üstüne seed yazmaz. Veritabanı yeni sürüm numarası içeriyorsa migration yazıları yapılmadan reddedilir.

İkinci migration gerektiğinde desteklenen maksimum sürümü artırın; her version adımını tek transaction ile yürütün. Varsayılan kayıt güncellemesini yalnızca user_defined=0 kayıtlarına uygulayın; kullanıcı override'larını koruyun. İlk migration yeni kurulumu, yeni migration mevcut kurulumu kapsamalı; upgrade/downgrade koruma testleri eklenmelidir. Şu an gerçek uygulanan migration yalnızca 1'dir; hayali gelecek migration dosyası yoktur.

Export settings, lockedTerms, userDefined rules ve lexicon içerir. Import boyut sınırı 2 MB'dir, bütün içerik transaction öncesi doğrulanır. Geçersiz import hiçbir eski kaydı değiştirmez. Kişisel kayıtlar birleştirilir, aynı ID/lemma kullanıcı kaydıyla güncellenir; history ve cache taşınmaz. DPAPI geçmişi başka bir Windows hesabına kopyalamak çözme yetkisi sağlamaz.

## Diagnostics ve test kanıtı

SafeLog sadece allowlist biçimine uyan olay kodu ve exception türü yazar. Message/stack trace/makine yolu/metin yazmaz. Günlük dosya rotasyonu ve yedi günlük retention vardır. Regresyon fixture'ları açık test metinleridir; geliştirici doğrulama raporundaki girdiler kullanıcı Word belgesi değildir.

Motor testleri citation, number, DOI/URL, kilit yumuşaması, gerçek çekim, belirsizlikte bırakma, alternatif/düzey/paragraf sınırları, OOXML run/biçim/field/link/tablo/story, DTD reddi, SQLite migration/atomik yedek/şifreleme/log/offline kontrollerini kapsar. Windows kabul betiği motor mock'u kullanmaz; yüklü VSTO COM automation nesnesine erişir ve gerçek Word belgesinde uygular. Linux'ta çalıştırılmış değildir.

## Gerçek Windows dağıtım kontrolü

`Verify-WindowsRuntime.ps1`, Windows PowerShell/.NET Framework üzerinde paketlenen gerçek `WindowsTextProtector` sınıfını yükleyip CurrentUser DPAPI geri çözme ve bozulmuş blob reddini çalıştırır; SQLite/JRE native PE mimarilerini, VSTO XML imzalarını ve manifest dosya özetlerini kontrol eder. Bu Word COM testi değildir. `Test-InstallerPrerequisites.ps1`, Word bulunmayan Windows runner’da gerçek Setup.exe’yi sessiz çalıştırıp erken önkoşul reddini ve eklenti kayıtlarının değişmediğini kontrol eder. Testler ilgili JSON kanıtlarını `artifacts` altında yazar; başarılı sayılmaları gerçek CI çıktısına bağlıdır.

Ürün kullanıcı kılavuzu installer’a dahil edilir; geliştirici README’si kaynakta kalır. `Smoke-Word.ps1`, gerçek Word sürümü/mimarisini PE başlığından okur ve kabul sonuçlarını JSON raporuna kaydeder. İnno önkoşul mesajları sessiz kurulumda da doğru sonuç koduyla durabilir.

## Word DLL önbelleği ve SQLite başlangıcı

VSTO, yönetilen DLL’leri .NET Framework önbelleğinden yükleyebilir. `Assembly.Location` bu durumda gerçek kurulum klasörü değildir. `InstallationDirectory`, dosya CodeBase’ini, kayıtlı yerel VSTO Manifest yolunu ve Location’ı denetleyerek Java/JAR içeren gerçek payload klasörünü bulur.

`NativeSqliteBootstrap`, Word işleminin bitness’ine uygun `runtimes/win-x86` veya `win-x64` SQLite DLL’sinin PE başlığını kontrol eder. Native dosyayı SQLitePCLRaw.core’un gerçek yükleme klasörüne atomik kopyalar; SHA256 eşleşiyorsa yeniden yazmaz. SQLitePCLRaw 2.1.13 net461 yükleyicisi böylece `WHERE_ADJACENT` aramasıyla dosyayı bulur. Üçüncü taraf DLL’ler ve provider değiştirilmez. Kişisel SQLite veritabanına bu aşamada erişilmez.

`Probe-Startup.ps1 -ShadowCopy -SkipNativeBootstrap`, düzeltme olmadan gerçek Framework önbelleği koşulunu sınar. `-ShadowCopy -Strict`, aynı koşulda ürünün gerçek başlangıç kodunu çalıştırır. `assemblyStorage: SHADOW` kontrolü, DLL’nin gerçekten önbellekten yüklendiğini doğrular. CI x86 ve x64, doğrudan ve önbellek yüklemesini ayrı Windows PowerShell süreçlerinde çalıştırır. `Tanila.cmd` geçici önbellekte tanılar; Word’ü açmaz. Bu test Word COM/Ribbon/biçim kabulü değildir.

Açılış tanısı hata aşaması, exception tipi, HRESULT, Word işlem mimarisi ve yalnızca ilk method’un adıyla sınırlıdır. Exception mesajı, dosya yolları, tam stack trace ve belge metni loglanmaz.

## 1.1.0 geniş sözlük ve korumalı cümle değişimleri

`python build/Prepare-LexicalData.py`, `build/lexical-data.json` içindeki sabit KeNet commit ve kaynak SHA256 değerini doğrular; kendi XML ayrıştırıcımızla `artifacts/lexical-data/kenet.sqlite` oluşturur. CC BY-SA 4.0 veri ve atıf bildirimi payload'a eklenir. Upstream GPL kodu kullanılmaz. Gerçek veride 78.327 anlam kümesi, 110.259 üyelik ve 82.155 farklı madde vardır. Paketlediğimiz indeksli SQLite yaklaşık 19,4 MB'dır. Kişisel veritabanından ayrı, salt okunur, 4 MiB SQLite sayfa önbelleği ile açılır; bütün sözlük belleğe alınmaz.

`WordNetLexicalSource`, tür ve anlam kümesini kontrol eder; çok anlamlı kaynak için tanım-bağlam örtüşmesi gerekir. Otomatik hedefler akademik kayıt filtresinden ve tek-anlam koşulundan geçer. Bu, tam semantik doğrulama veya bütün 82.155 sözcük için otomatik dönüşüm kapsamı değildir. Terimler ve özel isimler geniş sözlük sorgusundan önce çıkarılır; kullanıcının sözlük kayıtları önceliklidir. İnternet açıkken en fazla üç tek-sözcük kökü için toplam sekiz saniye bütçesiyle açık sözlük denenir; yerel anlam kümesine uymayan web karşılığı uygulanmaz.

`AnchoredRuleEdits`, kalıp içinde korunan sayı/terim/alan metnini sabit tutup çevresindeki değişimleri bölünmez bir öneri halinde üretir. Eksik/değişmiş veya sınırı aşan koruma tüm kalıbı reddeder. `CandidateIntegrity`, Word eşlemesinde her değişim uygulanabilir değilse tüm alternatifi reddeder; tekil edit silerek başka bir önizleme metnini uygulamaz.

Test için gerçek `ACADEMIC_JAVA`, `ACADEMIC_NLP_JAR` ve `ACADEMIC_WORDNET` yolları gereklidir. `tools/AcademicParaphraser.QualityProbe` dört argüman alır: KeNet SQLite, Java, JAR ve çıktı klasörü. Sentetik altı cümle/10 paragraf için gerçek dönüşüm metni ve süreç çalışma belleği raporlanır; Word açılmaz, metin örneğinin değişim sayısı genel doğruluk yüzdesi olarak yorumlanmaz.
