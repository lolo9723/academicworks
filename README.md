# Akademik Parafraz — Academic Paraphraser for Word

**1.6.0 çok aşamalı backend geliştirmesi:** Çekim/bağımlılık çözümleme, kuruluş üretimi, anlam denetimi, ayrı dil bilgisi ve nihai değerlendirme bileşenleri eklendi. Hazır büyük dil modeli zorunlu değildir. [Mimari, kaynaklar, gerçek küçük çözümleyici eğitimi ve sınırlar](docs/Backend-Architecture-1.6.0.md). Gerçek parafraz kalite kabulü ayrıca raporlanır.

**1.5.0 kalite kabulü tamamlanmadı:** Son yerel CPU sınamasında beş farklı paragrafın hiçbiri son kontrollerden geçmedi. 182 teknik testin geçmesi, doğal parafraz kalitesinin sağlandığı anlamına gelmez. [Tam kaynak/deneme/eleme raporu](docs/Local-Quality-1.5.0.md).

Windows masaüstü Microsoft Word için C#/.NET Framework 4.8 ve VSTO mimarisinde Türkçe/İngilizce parafraz projesi. 1.5.0 geliştirmesi, yeni kullanıcı yönlendirmesiyle yerel nicemlenmiş dil modelini cümle çözümleme, Word aralık planlaması ve ayrı anlam denetimine bağlar. Metin bilgisayarda işlenir; ücretli bulut servisi veya API anahtarı gerekmez. Önceki üretken-model yasağı bu yeni çalışma modu için değiştirilmiştir. Ayarlardan sınırlı kural motoru ayrıca seçilebilir.

## Teslim durumu

**1.4.0 yan cümle motoru:** Kelime konum hatası giderildi; bağlama göre morfolojik üretim, karşıtlık/bulgu/süreç yeniden kurma, anlam sinyali denetimi ve beş farklı alanın sabit sınaması eklendi. Aynı metinlerde 1.3.0 1/25, yeni motor 20/25 kaynak cümlesini değiştirdi. Bu oran anlam doğruluğu veya insan yazımı puanı değildir. [Öncesi/sonrası metinler, sorunlar ve sınırlar](docs/Clause-Engine-1.4.0.md).

**1.3.0 cümle motoru:** Varsayılan parafrazda sözlük eş anlamlıları ve eski kurallardaki tek yüklem değiştirmeleri uygulanmaz. 303 yeni cümle biçimi, klinik terim/kurum adı korumaları, sıra değişimini tanıyan alternatif denetimi ve cümle kapsamı gösterimi eklendi. [Gerçek motor çıktısı, sınırlar ve Windows doğrulaması](docs/Sentence-Engine-1.3.0.md). Bu sürüm hâlâ kural temellidir; her paragrafı serbestçe yeniden yazan bir dil modeli içermez.

**1.2.0 internet motoru:** “İnternetle Parafraz” komutu, uygun köklerin tamamını inceleyen ve eksiklerin tamamını internette arayan akış, Wikidata terim desteği ve SHA256 doğrulanan 25.104 birleşimlik cümle yapı bankası eklendi. [Canlı internet, gerçek çıktı ve Windows raporu](docs/Internet-Engine-1.2.0.md). Bu sayı bağımsız dilbilgisi ailelerinin sayısı değildir; her kelime/yapı için sonuç garantisi verilmez.

**1.1.0 geniş motor:** 82.155 maddelik indeksli KeNet verisi, toplam 1.252 kural, cümle/amaç/neden/yöntem dönüşümleri ve bölünmez korumalı edit önerileri eklendi. [Gerçek paragraf çıktısı, Windows ölçümleri ve kurulum](docs/Expanded-Engine-1.1.0.md). Geniş veri bütün maddeler için otomatik değiştirme garantisi değildir.

**1.0.2 açılış düzeltmesi:** Gerçek Windows Framework DLL önbelleğinde SQLite yükleme hatası yeniden oluşturuldu. Word işleminin mimarisine uygun native SQLite hazırlığı ve gerçek kurulum yolu çözümü eklendi. Önbellek ve doğrudan yükleme x86/x64 kontrolleri geçti. [Bulgu, test kanıtları ve kurulum](docs/Startup-Fix-1.0.2.md). Canlı Word sonucu henüz doğrulanmadı.

Gerçek Windows CI üzerinde Core, Infrastructure, .NET Framework WordHost ve VSTO WordAddin projeleri Debug/Release derlenmiş; gerçek SQLite ve Zemberek ile **152/152 motor testi her iki yapılandırmada geçmiştir**. `.vsto` ve `.dll.manifest` dosyaları imzalanmış, Inno Setup ile gerçek `AkademikParafraz-1.4.0-Setup.exe` üretilmiştir. Güncel doğrulanmış Windows koşusu: [37579070472](https://github.com/lolo9723/academicworks/actions/runs/37579070472). Windows DPAPI, native mimariler, VSTO XML imzaları/dosya özetleri ve eksik Word installer testi de geçti.

**Gerçek Word üzerinde kurulum, Ribbon yükleme, biçim/bağlantı/Undo/Track Changes kabulü henüz çalıştırılmamıştır.** Windows CI makinesinde Word yoktur. Bu ilk paket geliştirme yayıncı sertifikası kullanır; installer EXE için üretim Authenticode sertifikası sağlanmamıştır. Üretim kabulü tamamlanmış gibi sunulmaz. Son kullanıcı kılavuzu [UsersGuide.md](docs/UsersGuide.md), çalıştırılan kontroller [Verification.md](docs/Verification.md) ve canlı Word matrisi [Windows-Acceptance.md](docs/Windows-Acceptance.md) içindedir.

1.5.0 yerel modu aynı dilde Türkçe/İngilizce yeniden yazım içindir; çeviri modu henüz eklenmedi. 1.4.0 kural modu akademik Türkçe ile sınırlıdır. Kullanıcının kişisel üslubunu öğrenen bir model yoktur; kullanıcı sözlüğü ve kurallarıyla ifade tercihleri ayarlanır. Her metni baştan yazma veya kusursuz anlam eşdeğerliği garantisi verilmez.

## 1.5.0 gelişmiş motor

Bir defalık **2,5 GB** model indirmesi önizleme panelindeki düğmeyle yapılır. Paket CPU çalışma ortamını içerir; model dosyasını içermez. Qwen3-4B-Instruct-2507 Q4_K_M, sabit commit ve SHA256 ile doğrulanır. Model sunucusu yalnızca 127.0.0.1 adresine bağlanır, proxy kullanmaz ve her işlem bittiğinde kapatılır. Üretimden sonra ayrı bir model geçişi yeni bilgi, eksiltme, özne, olumsuzluk, neden ilişkisi ve kesinliği inceler. Otomatik kontrol anlam garantisi değildir. Her paragraf en fazla 2.400 karakter olmalıdır; uzun veya çok parçalı biçim seçimleri atlanabilir. [Teknik sınırlar ve kabul planı](docs/Local-Engine-1.5.0.md).

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
| Infrastructure / LexicalKnowledge | KeNet, tüm eksik köklerin internet taraması, SHA256 ile doğrulanmış ve gerektiğinde oluşturulan ek cümle kalıpları |
| Infrastructure / Persistence | SQLite migration, kural/sözlük/ayarlar, cache, şifreli geçmiş |
| Infrastructure / InternetDictionaryProviders | Anahtarsız açık sözlük sağlayıcıları, hata yalıtımı, offline cache |
| Infrastructure / Diagnostics | Metinsiz hata kodları ve günlük rotasyonu |
| nlp | Zemberek 0.17.1 çözümleyici ve çekim üretimi; Java 17 |
| Tests | Gerçek motor, koruma, OOXML ve veri regresyonları |
| Installer / build | Inno Setup tanımı, Windows derleme ve canlı Word test betikleri |

`AcademicParaphraser.sln` Windows/Visual Studio için tam solution'dır. `AcademicParaphraser.Engine.sln`, OfficeTools olmadan motor ve WordHost kütüphanesini derlemek/test etmek içindir; VSTO başlangıç projesini içermez. `tools/AcademicParaphraser.Verify` geliştirici doğrulama aracıdır, ayrı bir ürün editörü değildir.

## Belge koruma yaklaşımı ve sınırlar

Seçim metni ve `WordOpenXML` birlikte yakalanır. OOXML run'ları, biçim ve yapısal konteyner sınırlarıyla eşlenir. Field kodları, kaynakça/citation alanları, yer işaretleri, içerik denetimleri, mevcut revisions ve nesne metinleri korumaya alınır. Dönüşüm ancak eşleme tekil ve değişecek aralığın biçimi aynıysa kabul edilir. Birden fazla küçük edit içeren bir cümle alternatifi, editlerin tamamı bu denetimden geçerse uygulanabilir; biri reddedilirse tüm alternatif elenir.

Bütün seçimi `Range.Text` ile değiştirmek yerine yalnızca doğrulanmış küçük değişim aralıkları ters sırada düzenlenir. Font bilgisi saklanıp bu aralıklara uygulanır; paragraf sonları ve tablo hücresi işaretleri dönüşüme sokulmaz. Word COM işlemleri yalnızca yakalandıkları UI/STA iş parçacığında yapılır; morfoloji ve kural üretimi arka planda çalışır. En fazla 80.000 karakterlik seçim ve 16 MB OOXML sınırı vardır.

Farklı biçimlere yayılan bir kalıp, bağlantı sınırı, eşlenemeyen nesne işareti veya belirsiz seçim görülürse ilgili dönüşüm atlanır ya da seçim reddedilir. Bu tercih korumayı güçlendirir, parafraz kapsamını daraltır. SmartArt/şekil/denklem içeriği düzenlenmez. Dipnot, sonnot, üstbilgi ve altbilgi için ilgili paket parçasını seçen eşleme vardır; gerçek Word story davranışı kabul testinde doğrulanmalıdır. Satır sonu, sayfa/bölüm sonu, başlık/numaralandırma ve nesneleri bütün olası belgelerde koruduğu henüz kanıtlanmış değildir.

Sayı/atıf tespiti kapsamlı regex ve yerel morfoloji kullanır; yeni atıf biçimleri için özel koruma regex'i eklenebilir. Saptanan koruma aralığına dokunulmaz. Tanınmayan tüm atıf türlerini kesinlikle tanıdığı söylenemez.

## Kural ve sözlük kapsamı

1.3.0 yerel başlangıç verisi **1.555 tanımlı dönüşüm kuralı**, 77 yönetilebilir kişisel/teknik sözlük başlangıç kaydı ve ayrı, salt okunur **82.155 maddelik KeNet sözlük verisidir**. 61 temel kural korunmuş, `rule-templates.json` tablosundan 1.191 ek çekim ve cümle kalıbına 303 yeni cümle biçimi eklenmiştir. Varsayılan mod kelime/ifade değiştirme kayıtlarını filtreler; bütün tanımlı kayıtlar bu modda uygulanmaz. Bu sayı 1.555 bağımsız sentaks ailesi veya bütün sözlük maddelerini otomatik değiştirme yeteneği demek değildir. Sözlükte 13 isteğe bağlı morfolojik alternatif kaydı ve 64 teknik terim vardır. Sözlük/kural yöneticisi bu kayıtları SQLite üzerinde düzenler. Genel akademik Türkçe kuralları ile turizm, rekreasyon, sosyal bilimler, eğitim, psikoloji, işletme ve yönetim alan etiketleri/teknik terimleri bulunur; her alan için kapsamlı bir dil modeli veya uzmanlık korpusu bulunmaz.

Motor kalıp/fiil/yüklem dönüşümleri, açık öznesi bulunan etken-edilgen dönüşümleri, isim-fiil, isim tamlaması, yan cümle, sıfat-fiil, zarf-fiil, bağlayıcı ifadeler, güvenli sıralama, bulgu/yöntem/literatür/karşılaştırma/sonuç kalıplarını işler. Bunlar sınırlı ve açıkça tanımlı kalıplardır; genel Türkçe sentaksının tamamını dönüştüren bir parser değildir. Özne uydurulmaz; tanınmayan veya belirsiz yapı bırakılır.

Varsayılan parafraz cümle kuruluşuna öncelik verir; Orta amaç/yöntem/yan cümle kalıplarını, Güçlü ilave yeniden sıralamaları açar. Kelime önerileri Ayarlar’da özellikle açılmadıkça sözlük karşılıkları ve tek yüklem değiştirmeleri uygulanmaz. Hafif düzeyde güvenli cümle kalıbı bulunmazsa öneri oluşmayabilir. Güçlü modda da korumalar geçerlidir. Gereken çekim Zemberek'in gerçek analiz ve üretimiyle elde edilir; uyumlu çözümlemeler tek bir biçimde uzlaşmazsa sözcük değişmez. Yakın anlamlı kayıtlar otomatik eş anlamlı kabul edilmez.

İç güven puanları kural/sözlük uzman tahminleri ve yapısal fark sezgisidir; önizleme bunları bir anlam doğruluğu yüzdesi olarak göstermez. Ölçülmüş doğruluk yüzdesi veya matematiksel anlam eşdeğerliği kanıtı değildir. İnsani/akademik kalite açısından nihai öneri kullanıcı tarafından okunmalıdır. Güvenli değişim yoksa metin bırakılır. Alternatif sayısı hedeftir; yeterince farklı ve güvenli aday yoksa daha az öneri gösterilir.

## Gizlilik ve isteğe bağlı internet

Varsayılan ayarlar: internet kapalı, offline açık, korumalar açık, Orta düzey, üç alternatif, minimum kural güveni 0,92. Parafraz için internet gerekmez.

Sözlük ekranından kullanıcı tarafından girilen **tek kelime** isteğe bağlı olarak Türkçe Vikisözlük ve İngilizce Wiktionary'nin açık MediaWiki arayüzüne sorgulanabilir. Bunlar sonuç vermezse Wikidata'da tam Türkçe terim/alias kaydı denenir; terim açıklaması eş anlamlı sayılmaz. API anahtarı/hesap gerekmez. Kelime başına süre/boyut sınırı, kaynak kesintisinde beş dakika bekletme ve iptal desteği vardır; bulunan/eksik kök sayısı önizlemede gösterilir. Paragraf dışarı gönderilmez. Kullanıcı “İnternetle Parafraz” komutunu kullanırsa veya interneti açıp offline modu kapatırsa bütün uygun benzersiz kök/tür çiftleri incelenir, yerel karşılığı eksik kalanların tamamı internet kaynaklarında aranır; bu bilgiler varsayılan parafrazda kelime değişimi olarak uygulanmaz. Kelime önerileri özellikle açılmışsa karşılık yerel anlam kümesi ve kayıt filtresinden geçmelidir; öneri ayrıca kullanıcı tarafından kontrol edilir. Sözcük bilgisi panelde kaynağıyla gösterilir; internetteki bilgi körlemesine otomatik dönüşüm kuralı yapılmaz. Kullanıcı uygun karşılığı yerel sözlüğe ekleyebilir.

İnternet açık olsa bile offline mod açıkken ağ isteği yapılmaz. Cache 30 gün geçerlidir. Tek bir sağlayıcının hata vermesi parafrazı durdurmaz. İnternette bulunma, ücretsiz erişim ve sağlayıcının gelecekteki sürekliliği garanti değildir.

Geçmişte yalnızca uygulanan dönüşümler, Windows kullanıcısına bağlı DPAPI ile şifrelenerek saklanır; son 200 kayıt tutulur. Tanılama logları metin, exception mesajı veya stack trace içermez. Loglar günlük döner, yedi gün tutulur. Kural/ayar/terim JSON yedekleri şifreli değildir; geçmiş yedeğe dahil değildir. Kullanıcı kendi eklediği terimler ve kuralları dosyadan okuyabilir.

## Geliştirici gereksinimleri

- Windows 10/11 x64, masaüstü Word 2016 veya daha yeni / Microsoft 365; 32 ve 64 bit Office için AnyCPU tasarımı. Gerçek bitness matrisi henüz test edilmedi.
- Visual Studio 2022, **Office/SharePoint development** workload / VSTO bileşeni (`Microsoft.VisualStudio.Component.TeamOffice`) ve .NET Framework 4.8 Developer Pack. Build Tools için karşılık `Microsoft.VisualStudio.Component.TeamOffice.BuildTools`dur.
- Python 3.9+ (yalnızca derlemede veri ve kural hazırlığı), .NET SDK 8, Maven 3.9+, Java **JDK 17+**; `dotnet`, `mvn`, `java` PATH içinde ve `JAVA_HOME` JDK'yı göstermeli.
- Inno Setup **6.3+** (`ArchitecturesAllowed=x64compatible` kullanımı için).
- Microsoft VSTO Runtime. [Microsoft indirme](https://www.microsoft.com/en-us/download/details.aspx?id=105522).

Son kullanıcıya JDK, Maven, SDK veya Visual Studio gerekmemesi hedeflenir: Windows paketleme betiği doğrulanmış JRE'yi ve NLP JAR'ını payload'a dahil eder; Java sidecar'ı eklenti açar/kapatır. Son kullanıcıdaki .NET 4.8, Word ve VSTO Runtime kontrolleri installer tanımında vardır.

## Windows derleme ve paketleme

Geliştirici PowerShell oturumunda, Word kapalıyken:

```powershell
.\build\Build-Windows.ps1
```

Betik NLP JAR'ını derler, sabitlenmiş KeNet verisini kaynak SHA256 ile doğrulayıp indeksli SQLite'a dönüştürür, `build/java-runtime.json` içindeki sabit Temurin Windows JRE arşivini indirip SHA256 doğrular, motor solution'ını restore eder, Debug/Release uyarısız derleme ve testleri çalıştırır, VSTO başlangıç projesini MSBuild ile derleyip manifest imzalar, native SQLite x86/x64 ve Java dosyalarını doğrular, payload'ı Inno Setup ile paketler.

Üretim yayıncı sertifikası mevcutsa:

```powershell
.\build\Build-Windows.ps1 -SigningThumbprint 'KOD_IMZALAMA_SERTIFIKASININ_THUMBPRINT_DEGERI'
```

Sertifika verilmezse geliştirme code-signing sertifikası oluşturulur. Kendinden imzalı geliştirme sertifikası otomatik güvenilir yapılmaz. VSTO'nun yayıncı/güven uyarısı çıkabilir; kurum politikasının bloklaması derleme ile çözülmüş sayılmaz. Setup.exe üretim Authenticode imzası bu teslimde üretilmemiştir. VSTO manifest imzası, installer exe imzasının yerine geçmez.

Başarı çıktısı: `artifacts/installer/AkademikParafraz-1.4.0-Setup.exe`. **Bu dosya gerçek Windows CI koşusunda oluşturulmuştur.** VSTO hedefleri olmayan sistemde tam WordAddin derlemesi geçmez. Betik eksik artefaktları atlayıp başarı bildirmez.

## Windows otomatik derleme

`.github/workflows/windows-build.yml` her push veya manuel çalıştırmada `windows-2022` üzerinde çalışır. Gerçek Visual Studio VSTO bileşenini `Prepare-WindowsCI.ps1` ile tamamlar; Zemberek, motor ve VSTO projelerini Debug/Release derleyip test eder, manifestleri geliştirme sertifikasıyla imzalar, Inno Setup installer üretir. Başarılı koşu `AkademikParafraz-1.4.0-Windows-Setup` artefaktını, SHA256 dosyasını ve yayıncı açık sertifikasını sunar. İmzalama özel anahtarı artefakta dahil edilmez.

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
python build/Prepare-LexicalData.py
export ACADEMIC_WORDNET="$PWD/artifacts/lexical-data/kenet.sqlite"
bash build/Test-Local.sh
```

Windows PowerShell karşılığı:

```powershell
mvn -f nlp/pom.xml clean package
$env:ACADEMIC_JAVA = 'C:\JDK\bin\java.exe'
$env:ACADEMIC_NLP_JAR = "$PWD\nlp\target\turkish-nlp-1.0.0.jar"
python build/Prepare-LexicalData.py
$env:ACADEMIC_WORDNET = "$PWD\artifacts\lexical-data\kenet.sqlite"
dotnet restore AcademicParaphraser.Engine.sln
dotnet build AcademicParaphraser.Engine.sln -c Release -warnaserror
dotnet test tests/AcademicParaphraser.Tests -c Release --no-build
```

`ACADEMIC_JAVA`, `ACADEMIC_NLP_JAR` ve `ACADEMIC_WORDNET` gerçek dosyalara işaret etmezse testler hata verir; sahte NLP ile sessiz başarı yoktur. Linux'ta derlenen WordHost DLL'i canlı Word'ün çalıştırıldığı anlamına gelmez.

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

SQLite migration 1; settings, rules, lexicon, locked_terms, dictionary_cache, history ve migrations tablolarını oluşturur. Parametreli SQL, transaction ile atomik import ve yeni şema sürümünün açılmasını engelleme vardır. Başlangıç verileri mevcut kullanıcı kaydının üstüne yazılmaz; yeni genişletilmiş kural ID'leri INSERT OR IGNORE ile mevcut kurulumlara da eklenir. Yeni migration ve seed güncelleme politikası geliştirici belgesinde açıklanmıştır.

Yeni kural/sözlük/provider eklemek, morphology protokolü, koruma sınırları ve versiyon değişiklikleri için [Developer.md](docs/Developer.md) dosyasını kullanın. Üçüncü taraf bileşen ve lisansları [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) içinde listelenmiştir.
