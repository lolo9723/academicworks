# Akademik Parafraz — Academic Paraphraser for Word

Windows masaüstü Microsoft Word için C#/.NET Framework 4.8 ve VSTO mimarisinde, yerel Zemberek morfolojisi ve yönetilebilir akademik dönüşüm kuralları kullanan Türkçe parafraz projesi. Belgeyi bir sunucuya göndermez. LLM, üretken yapay zekâ, ücretli model servisi veya model API anahtarı kullanmaz.

## Teslim durumu

Bu teslim **Windows üzerinde kurulup doğrulanmış bir ürün olarak sertifikalandırılmış değildir**. Geliştirme ortamı Linux'tur. C# motoru, veri katmanı, .NET Framework WordHost kütüphanesi ve gerçek Zemberek sidecar derlenmiştir. Otomatik testler gerçek SQLite ve gerçek Zemberek kullanır. VSTO başlangıç ve Ribbon C# kaynakları ayrıca Microsoft’un sabit SHA256 ile doğrulanan gerçek SDK referanslarıyla Debug/Release derlenmiştir; bu kontrol manifest üretimi değildir. Tam VSTO proje derlemesi, `.vsto` manifestleri, Windows installer üretimi ve canlı Word kabul testleri bu ortamda henüz doğrulanamamıştır. Tam VSTO derlemesi, OfficeTools MSBuild hedeflerinin Linux'ta bulunmaması nedeniyle başarısız olmuştur.

Kaynak paketi, Windows derleme/paketleme betiği, installer tanımı, testler ve doğrulama kanıtları teslim edilir. **Kaynak ZIP'i bir kurulum dosyası değildir; bu teslimde hazır Setup.exe/MSI yoktur.** Gerçek Word yükleme, biçim, Undo ve Track Changes kontrolleri geçmeden üretim sürümü olarak dağıtılmamalıdır. Çalıştırılan ve çalıştırılmayan kontroller [doğrulama raporunda](docs/Verification.md) ayrılmıştır.

Bu sürümün kapsamı son şartnameye göre **akademik Türkçe parafrazdır**. İngilizce çeviri veya İngilizce parafraz motoru içermez. İngilizce Wiktionary sağlayıcısı sadece isteğe bağlı kelime bilgisi içindir. Kullanıcının kişisel üslubunu öğrenen bir model yoktur; kullanıcı sözlüğü ve kurallarıyla ifade tercihleri ayarlanır. Her metni baştan yazma veya kusursuz anlam eşdeğerliği garantisi verilmez.

## Word kullanım akışı

Windows derlemesi ve kurulumu doğrulandıktan sonra:

1. Word'de cümle, paragraf veya birkaç paragraf seçin.
2. **AKADEMİK PARAFRAZ > Hafif / Orta / Güçlü > Parafraz Et** komutunu kullanın.
3. Yerel analiz sırasında küçük panelde işlem durumu görülür. **İptal** belgeye yazmadan işlemi durdurur.
4. Eski ve yeni ifadeyi, değişen bölümleri ve alternatifleri inceleyin.
5. **Uygula** seçilen öneriyi küçük metin aralıkları halinde yazar. Öneri üretme belgeyi değiştirmez.
6. Tek **Ctrl+Z** işlemi hedeflenir; eklentinin **Geri Al** komutu da son belge imzasını kontrol eder.

Ribbon; alternatif üretme/gezinme, önizleme, atıf/sayı/özel isim/teknik terim/bağlantı korumaları, terim kilitleme, ayarlar, sözlük, geçmiş ve değişiklik izleme komutlarını içerir. Ribbon'un gerçek Word'de görünmesi Windows kabul testine tabidir.

**Bağlantıları Koru** açıkken görünen bağlantı metni de kilitlenir. Bağlantı içindeki görünen metni değiştirmek için bu seçeneği kapatıp metni seçin. URL hedefi ve alan kodu yine korunur; bağlantı sınırını aşan dönüşüm reddedilir. URL hedefleri uygulamadan önce/sonra karşılaştırılır ve fark varsa işlem geri alınır. Bu mekanizmanın gerçek Word field davranışı henüz doğrulanmamıştır.

**Terimi Kilitle** seçili ifadeyi kişisel koruma sözlüğüne ekler. “Öznel zindelik”, “boş zaman” ve “etkinlik doyumu” gibi teknik ifadeler başlangıç sözlüğünde de yer alır. Kilitli ifadelerin bazı ekli biçimleri ve ünsüz yumuşaması korunur; bütün özel isimleri eksiksiz tanıdığı iddia edilmez.

## Mimari

| Katman | Sorumluluk |
| --- | --- |
| WordAddin | VSTO yaşam döngüsü, Ribbon, task pane, COM kabul testi arayüzü |
| WordHost | Word STA erişimi, seçim yakalama, native küçük metin düzenlemeleri, WinForms panelleri |
| Core / ParaphraseEngine / RuleEngine | Deterministik kural seçimi, morfolojik değişimler, farklı alternatifler, güven eşikleri |
| Core / CitationProtection | Atıf, sayı, tanımlayıcı, özel isim ve kilit koruması |
| Core / DocumentProtection | OOXML metin/run eşleme, biçim/alan/bağlantı sınırları |
| Infrastructure / TurkishNlp | Otomatik Java sidecar yönetimi, iptal/zaman aşımı, UTF-8 stdio protokolü |
| Infrastructure / Persistence | SQLite migration, kural/sözlük/ayarlar, cache, şifreli geçmiş |
| Infrastructure / InternetDictionaryProviders | Anahtarsız açık sözlük sağlayıcıları, hata yalıtımı, offline cache |
| Infrastructure / Diagnostics | Metinsiz hata kodları ve günlük rotasyonu |
| nlp | Zemberek 0.17.1 çözümleyici ve çekim üretimi; Java 17 |
| Tests | Gerçek motor, koruma, OOXML ve veri regresyonları |
| Installer / build | Inno Setup tanımı, Windows derleme ve canlı Word test betikleri |

`AcademicParaphraser.sln` Windows/Visual Studio için tam solution'dır. `AcademicParaphraser.Engine.sln`, OfficeTools olmadan motor ve WordHost kütüphanesini derlemek/test etmek içindir; VSTO başlangıç projesini içermez. `tools/AcademicParaphraser.Verify` geliştirici doğrulama aracıdır, ayrı bir ürün editörü değildir.

## Belge koruma yaklaşımı ve sınırlar

Seçim metni ve `WordOpenXML` birlikte yakalanır. OOXML run'ları, biçim ve yapısal konteyner sınırlarıyla eşlenir. Field kodları, kaynakça/citation alanları, yer işaretleri, içerik denetimleri, mevcut revisions ve nesne metinleri korumaya alınır. Dönüşüm ancak eşleme tekil ve değişecek aralığın biçimi aynıysa kabul edilir.

Bütün seçimi `Range.Text` ile değiştirmek yerine yalnızca doğrulanmış küçük değişim aralıkları ters sırada düzenlenir. Font bilgisi saklanıp bu aralıklara uygulanır; paragraf sonları ve tablo hücresi işaretleri dönüşüme sokulmaz. Word COM işlemleri yalnızca yakalandıkları UI/STA iş parçacığında yapılır; morfoloji ve kural üretimi arka planda çalışır. En fazla 80.000 karakterlik seçim ve 16 MB OOXML sınırı vardır.

Farklı biçimlere yayılan bir kalıp, bağlantı sınırı, eşlenemeyen nesne işareti veya belirsiz seçim görülürse ilgili dönüşüm atlanır ya da seçim reddedilir. Bu tercih korumayı güçlendirir, parafraz kapsamını daraltır. SmartArt/şekil/denklem içeriği düzenlenmez. Dipnot, sonnot, üstbilgi ve altbilgi için ilgili paket parçasını seçen eşleme vardır; gerçek Word story davranışı kabul testinde doğrulanmalıdır. Satır sonu, sayfa/bölüm sonu, başlık/numaralandırma ve nesneleri bütün olası belgelerde koruduğu henüz kanıtlanmış değildir.

Sayı/atıf tespiti kapsamlı regex ve yerel morfoloji kullanır; yeni atıf biçimleri için özel koruma regex'i eklenebilir. Saptanan koruma aralığına dokunulmaz. Tanınmayan tüm atıf türlerini kesinlikle tanıdığı söylenemez.

## Kural ve sözlük kapsamı

Başlangıç verisi `src/AcademicParaphraser.Core/Data` altında **61 kural ve 39 sözlük kaydıdır**. Sözlükte 13 morfolojik alternatif kaydı ve 26 teknik terim vardır. Sözlük/kural yöneticisi bu kayıtları SQLite üzerinde düzenler. Genel akademik Türkçe kuralları ile turizm, rekreasyon, sosyal bilimler, eğitim, psikoloji, işletme ve yönetim alan etiketleri/teknik terimleri bulunur; her alan için kapsamlı bir dil modeli veya uzmanlık korpusu bulunmaz.

Motor kalıp/fiil/yüklem dönüşümleri, açık öznesi bulunan etken-edilgen dönüşümleri, isim-fiil, isim tamlaması, yan cümle, sıfat-fiil, zarf-fiil, bağlayıcı ifadeler, güvenli sıralama, bulgu/yöntem/literatür/karşılaştırma/sonuç kalıplarını işler. Bunlar sınırlı ve açıkça tanımlı kalıplardır; genel Türkçe sentaksının tamamını dönüştüren bir parser değildir. Özne uydurulmaz; tanınmayan veya belirsiz yapı bırakılır.

Hafif mod küçük/lexical kalıpları, Orta ilave sentaktik kalıpları, Güçlü tanımlı yeniden sıralamaları açar. Güçlü modda da korumalar geçerlidir. Gereken çekim Zemberek'in gerçek analiz ve üretimiyle elde edilir; uyumlu çözümlemeler tek bir biçimde uzlaşmazsa sözcük değişmez. Yakın anlamlı kayıtlar otomatik eş anlamlı kabul edilmez.

Güven puanları kural/sözlük uzman tahminleri ve yapısal fark sezgisidir. Ölçülmüş doğruluk yüzdesi veya matematiksel anlam eşdeğerliği kanıtı değildir. İnsani/akademik kalite açısından nihai öneri kullanıcı tarafından okunmalıdır. Güvenli değişim yoksa metin bırakılır. Alternatif sayısı hedeftir; yeterince farklı ve güvenli aday yoksa daha az öneri gösterilir.

## Gizlilik ve isteğe bağlı internet

Varsayılan ayarlar: internet kapalı, offline açık, korumalar açık, Orta düzey, üç alternatif, minimum kural güveni 0,92. Parafraz için internet gerekmez.

Sözlük ekranından kullanıcı tarafından girilen **tek kelime** isteğe bağlı olarak Türkçe Vikisözlük ve İngilizce Wiktionary'nin açık MediaWiki arayüzüne sorgulanabilir. API anahtarı/hesap gerekmez. Seçim metni otomatik alınmaz ve dışarı gönderilmez. Sözcük bilgisi panelde kaynağıyla gösterilir; internetteki bilgi körlemesine otomatik dönüşüm kuralı yapılmaz. Kullanıcı uygun karşılığı yerel sözlüğe ekleyebilir.

İnternet açık olsa bile offline mod açıkken ağ isteği yapılmaz. Cache 30 gün geçerlidir. Tek bir sağlayıcının hata vermesi parafrazı durdurmaz. İnternette bulunma, ücretsiz erişim ve sağlayıcının gelecekteki sürekliliği garanti değildir.

Geçmişte yalnızca uygulanan dönüşümler, Windows kullanıcısına bağlı DPAPI ile şifrelenerek saklanır; son 200 kayıt tutulur. Tanılama logları metin, exception mesajı veya stack trace içermez. Loglar günlük döner, yedi gün tutulur. Kural/ayar/terim JSON yedekleri şifreli değildir; geçmiş yedeğe dahil değildir. Kullanıcı kendi eklediği terimler ve kuralları dosyadan okuyabilir.

## Geliştirici gereksinimleri

- Windows 10/11 x64, masaüstü Word 2016 veya daha yeni / Microsoft 365; 32 ve 64 bit Office için AnyCPU tasarımı. Gerçek bitness matrisi henüz test edilmedi.
- Visual Studio 2022, **Office/SharePoint development** workload / VSTO bileşeni (`Microsoft.VisualStudio.Component.TeamOffice`) ve .NET Framework 4.8 Developer Pack. Build Tools için karşılık `Microsoft.VisualStudio.Component.TeamOffice.BuildTools`dur.
- .NET SDK 8, Maven 3.9+, Java **JDK 17+**; `dotnet`, `mvn`, `java` PATH içinde ve `JAVA_HOME` JDK'yı göstermeli.
- Inno Setup **6.3+** (`ArchitecturesAllowed=x64compatible` kullanımı için).
- Microsoft VSTO Runtime. [Microsoft indirme](https://www.microsoft.com/en-us/download/details.aspx?id=105522).

Son kullanıcıya JDK, Maven, SDK veya Visual Studio gerekmemesi hedeflenir: Windows paketleme betiği doğrulanmış JRE'yi ve NLP JAR'ını payload'a dahil eder; Java sidecar'ı eklenti açar/kapatır. Son kullanıcıdaki .NET 4.8, Word ve VSTO Runtime kontrolleri installer tanımında vardır.

## Windows derleme ve paketleme

Geliştirici PowerShell oturumunda, Word kapalıyken:

```powershell
.\build\Build-Windows.ps1
```

Betik NLP JAR'ını derler, `build/java-runtime.json` içindeki sabit Temurin Windows JRE arşivini indirip SHA256 doğrular, motor solution'ını restore eder, Debug/Release uyarısız derleme ve testleri çalıştırır, VSTO başlangıç projesini MSBuild ile derleyip manifest imzalar, native SQLite x86/x64 ve Java dosyalarını doğrular, payload'ı Inno Setup ile paketler.

Üretim yayıncı sertifikası mevcutsa:

```powershell
.\build\Build-Windows.ps1 -SigningThumbprint 'KOD_IMZALAMA_SERTIFIKASININ_THUMBPRINT_DEGERI'
```

Sertifika verilmezse geliştirme code-signing sertifikası oluşturulur. Kendinden imzalı geliştirme sertifikası otomatik güvenilir yapılmaz. VSTO'nun yayıncı/güven uyarısı çıkabilir; kurum politikasının bloklaması derleme ile çözülmüş sayılmaz. Setup.exe Authenticode imzası ve kurumsal dağıtım onayı bu teslimde üretilmemiştir. VSTO manifest imzası, installer exe imzasının yerine geçmez.

Beklenen başarı çıktısı: `artifacts/installer/AkademikParafraz-1.0.0-Setup.exe`. **Bu dosya mevcut teslimde oluşturulmamıştır.** VSTO hedefleri olmayan sistemde tam WordAddin derlemesi geçmez. Betik eksik artefaktları atlayıp başarı bildirmez.

## Windows otomatik derleme

`.github/workflows/windows-build.yml` her push veya manuel çalıştırmada `windows-2022` üzerinde çalışır. Gerçek Visual Studio VSTO bileşenini `Prepare-WindowsCI.ps1` ile tamamlar; Zemberek, motor ve VSTO projelerini Debug/Release derleyip test eder, manifestleri geliştirme sertifikasıyla imzalar, Inno Setup installer üretir. Başarılı koşu `AkademikParafraz-1.0.0-Windows-Setup` artefaktını, SHA256 dosyasını ve yayıncı açık sertifikasını sunar. İmzalama özel anahtarı artefakta dahil edilmez.

GitHub Windows runner’da masaüstü Word yoktur; başarılı CI çalışması Word kabul testlerinin yerine geçmez. CI, installer üretiminden önce gerçek VSTO ve bütün bağımlılık çıktıları bulunmadığında başarısız olur. Action sürümleri commit SHA ile sabitlenmiştir; NuGet restore kilit dosyaları kullanır.

## VSTO kaynak/API kontrolü

Windows manifest üretiminden ayrı olarak, Python 3.9+ ve .NET SDK ile gerçek SDK referans kontrolü çalıştırılabilir:

```bash
python build/Check-VstoSources.py --configuration Debug
python build/Check-VstoSources.py --configuration Release
```

Betik Microsoft’un `build/vsto-sdk.json` içindeki resmi VSIX paketini indirip SHA256 doğrular; WordAddin projesindeki kaynakları, kaynakları yeniden yazmadan, beyan edilen referanslar ve interop ayarlarıyla derler. SDK geliştirici cache’inde kalır ve ürüne dağıtılmaz. Bu yöntem `.vsto` manifesti veya installer oluşturmaz ve Word çalıştırmaz; sadece gerçek API uyumluluğunu kontrol eder.

## Motoru Word olmadan derleme/test

```bash
mvn -f nlp/pom.xml clean package
export ACADEMIC_JAVA="/absolute/path/to/java"
export ACADEMIC_NLP_JAR="$PWD/nlp/target/turkish-nlp-1.0.0.jar"
bash build/Test-Local.sh
```

Windows PowerShell karşılığı:

```powershell
mvn -f nlp/pom.xml clean package
$env:ACADEMIC_JAVA = 'C:\JDK\bin\java.exe'
$env:ACADEMIC_NLP_JAR = "$PWD\nlp\target\turkish-nlp-1.0.0.jar"
dotnet restore AcademicParaphraser.Engine.sln
dotnet build AcademicParaphraser.Engine.sln -c Release -warnaserror
dotnet test tests/AcademicParaphraser.Tests -c Release --no-build
```

`ACADEMIC_JAVA` ve `ACADEMIC_NLP_JAR` gerçek dosyalara işaret etmezse testler hata verir; sahte NLP ile sessiz başarı yoktur. Linux'ta derlenen WordHost DLL'i canlı Word'ün çalıştırıldığı anlamına gelmez.

```bash
dotnet run --project tools/AcademicParaphraser.Verify -c Release --no-build
# Açık kaynakta yalnızca örnek "yöntem" kelimesini canlı denemek için:
dotnet run --project tools/AcademicParaphraser.Verify -c Release --no-build -- --online-dictionary
```

Bu araç yalnızca şartnamedeki örnek ve açık doğrulama girdileriyle gerçek çıktı/süre raporu üretir; kullanıcı belgesi okumaz. TRX ve doğrulama raporu `artifacts` altında oluşturulur, kaynak kontrolüne alınmaz.

## Kurulum/kaldırma ve kabul

Installer tanımı kullanıcı başına `%LOCALAPPDATA%\Programs\AkademikParafraz` içine yükler, `HKCU\Software\Microsoft\Office\Word\Addins` kaydını oluşturur ve VSTO Installer ile manifest güven doğrulamasını başlatır. Office 32/64 kayıt yönlendirmesi ve native bağımlılık yüklemesi canlı testte onaylanmalıdır. Normal Windows **Yüklü Uygulamalar** kaldırma kaydı Inno Setup tarafından oluşturulur. Kullanıcının sözlük/ayar/geçmiş verileri kaldırmada saklanır; bunların yeri `%LOCALAPPDATA%\AkademikParafraz`dır.

Ürün kabul adımları [Windows-Acceptance.md](docs/Windows-Acceptance.md), otomatik başlangıç testi `build/Smoke-Word.ps1` içindedir. Bu betik gerçek Word ve kurulu eklentiyi gerektirir; geçici bir belge açar ve kaydetmeden kapatır. Mevcut belgeleri kapatmanızı ister, onları kapatıp kaybetmez.

## Hata ayıklama, veritabanı ve genişletme

Word'de Ayarlar > Tanılama; Word sürümü, işlem bitness'i, eklenti sürümü, NLP/veritabanı/sözlük durumunu gösterir. Geliştirici, WordAddin projesini Visual Studio'da açıp Word'e debug ile bağlanabilir. Loglar `%LOCALAPPDATA%\AkademikParafraz\logs` altındadır. Protected/salt okunur belge, boş/görsel seçim veya güvenle eşlenemeyen alan kullanıcıya açıklanır; stack trace gösterilmez.

SQLite migration 1; settings, rules, lexicon, locked_terms, dictionary_cache, history ve migrations tablolarını oluşturur. Parametreli SQL, transaction ile atomik import ve yeni şema sürümünün açılmasını engelleme vardır. Başlangıç verileri mevcut kullanıcı kaydının üstüne yazılmaz. Yeni migration ve seed güncelleme politikası geliştirici belgesinde açıklanmıştır.

Yeni kural/sözlük/provider eklemek, morphology protokolü, koruma sınırları ve versiyon değişiklikleri için [Developer.md](docs/Developer.md) dosyasını kullanın. Üçüncü taraf bileşen ve lisansları [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) içinde listelenmiştir.
