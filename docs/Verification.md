# Akademik Parafraz — doğrulama raporu

Tarih: 2026-10-06. Ürün/kaynak sürümü: 1.0.0.

## Teslim durumu

Gerçek Windows 2022 / Visual Studio 2022 OfficeTools üzerinde tam VSTO eklentisi Debug ve Release derlendi, uygulama/deployment manifestleri imzalandı ve Inno Setup ile gerçek Windows Setup.exe üretildi. Canlı Microsoft Word bulunan bir makine bu oturuma bağlı olmadığından Word içinde kurulum, Ribbon, biçim, bağlantı, geri alma ve değişiklik izleme kabulü çalıştırılmadı. Üretim kabulü tamamlanmış olarak sunulmaz.

Son Windows CI koşusu: [37421601160](https://github.com/lolo9723/academicworks/actions/runs/37421601160) — **success**. Derlenen kaynak commit'i: `697af87861851ab68a563ea3e47087627e1bdfc6`.

Installer: `AkademikParafraz-1.0.0-Setup.exe` — 39,179,373 byte.

SHA256: `54cca46c444fb7ba17cf4e9fca0bf91193c4bd096d67eb488a9a62cf6ed75c8c`.

İndirilen aktarım ZIP'leri GitHub artifact SHA256 özetleriyle; yeniden birleştirilen EXE, Windows'ta hesaplanmış EXE SHA256 özetiyle birebir doğrulandı. Teslim kurulum ZIP'i: `AkademikParafraz-1.0.0-Windows-kurulum.zip`, SHA256 `27af9bcece1d383762c6f60ba40feaffe71458c09337e44c8a7eeb64bc4b22d1`.

Windows manifest kontrolü: uygulama manifestindeki **343 dosya** ve deployment manifestindeki **1 referans** hash karşılaştırmasından geçti. Her iki XML imzası geçerli. Windows DPAPI roundtrip ve değiştirilmiş ciphertext reddi geçti. Kanıt JSON'larında `wordExecuted: false` olarak kayıtlıdır.

## Gerçek Windows kontrolleri

- Tam `AcademicParaphraser.sln`: gerçek Microsoft OfficeTools/VSTO hedefleriyle Debug/Release, uyarılar hata sayılarak derlendi. Eksik SDK hedefi taklit edilmedi.
- Gerçek Java 17 ve Zemberek 0.17.1 sidecar'ı, gerçek SQLite dosyalarıyla motor testleri her iki yapılandırmada çalıştı.
- Gerçek .NET Framework 4.8 altında Windows CurrentUser DPAPI şifreleme/çözme ve değiştirilmiş ciphertext reddi geçti.
- SQLite x86/x64 DLL'lerinin ve bundled Java x64 EXE'sinin gerçek PE mimarileri doğrulandı.
- `.dll.manifest` ve `.vsto` XML RSA-SHA256 imzaları ve referans edilen dosyaların hashleri gerçek kriptografik kontrollerden geçti. Bu kontrol sertifikanın kullanıcının/kurumun Windows güven deposunda güvenilir olduğu anlamına gelmez.
- Inno Setup 6.7.1 gerçek installer oluşturdu. Word bulunmayan Windows üzerinde gerçek EXE sessiz çalıştırıldı; eksik Word mesajıyla exit 1 verdi ve Word eklenti kaydı oluşturmadan durdu. Başarılı kurulum/kaldırma yolu canlı Word kabulüne tabidir.

| Windows yapılandırması | Toplam | Geçti | Başarısız | Atlandı |
| --- | ---: | ---: | ---: | ---: |
| Debug | 64 | 64 | 0 | 0 |
| Release | 64 | 64 | 0 | 0 |

Testler atıf/istatistik/sayı/teknik terim koruması, gerçek Türkçe çekim üretimi, belirsizliği bırakma, farklı düzey/alternatifler, paragraf bağlamı, run/biçim/link sınırları, alanlar, tablo/story/nesne işaretleri, DTD reddi, migration, atomik yedek, geçersiz veri reddi, şifreli geçmiş ve offline davranışını kapsar. OOXML ve motor testleri canlı Word UI/COM davranışının yerine geçmez.

## Linux geliştirme kontrolleri

Debian 13, .NET SDK 8.0.425 ve gerçek Java 17 ortamında motor/WordHost Debug/Release sıfır warning/error ve 64/64 test sonucu elde edildi. Resmi Microsoft VS2022 kataloğundan indirilen ve SHA256 doğrulanan VSTO SDK referanslarıyla WordAddin kaynak/API kontrolleri de geçti.

İlk Linux tam VSTO denemesi OfficeTools hedefi bulunmadığından MSB4019 ile durmuştu. Bu tarihsel log kanıt arşivinde tutulmuştur; güncel Windows tam VSTO derlemesi başarıyla tamamlanmıştır.

## Şartnamedeki koruma örneği

Girdi:

> Doğayla ilişkinin öznel zindelik üzerindeki etkisinin anlamlı olduğu belirlenmiştir (Yılmaz & Demir, 2024; β=.43, p<.001).

Gerçek motorun Orta düzeydeki çıktılarından biri:

> Doğayla ilişkinin öznel zindelik üzerindeki etkisinin anlamlı olduğu saptanmıştır (Yılmaz & Demir, 2024; β=.43, p<.001).

Atıf/istatistik dizisi ve kilitli terim birebir korundu. Bu örnekte iki farklı güvenli aday bulundu; istenen üç adayı uydurarak tamamlama yapılmadı. Metnin tamamı yeniden yazılmadı; güvenli kalıp değişti.

## Süre ölçümü

| Girdi | Düzey | Süre (ms) | Alternatif |
| --- | --- | ---: | ---: |
| 1 cümle — şartnamedeki koruma örneği | Light | 1008 | 2 |
| 1 cümle — şartnamedeki koruma örneği | Moderate | 50 | 2 |
| 1 cümle — şartnamedeki koruma örneği | Strong | 8 | 2 |
| 1 paragraf — doğrulama girdisi | Light | 13 | 3 |
| 1 paragraf — doğrulama girdisi | Moderate | 8 | 3 |
| 1 paragraf — doğrulama girdisi | Strong | 14 | 3 |
| 10 paragraf — doğrulama girdisi | Light | 75 | 3 |
| 10 paragraf — doğrulama girdisi | Moderate | 85 | 3 |
| 10 paragraf — doğrulama girdisi | Strong | 44 | 3 |

İlk cümle ölçümü NLP cold start'ı da içerir. Diğer ölçümler sıcak motoradır. Girdiler şartnamedeki örnek ve geliştirici doğrulama metinleridir. Bu tek ortam/tek koşu ölçümüdür; Word COM/UI gecikmesini içermez ve farklı bilgisayarlar için hız garantisi değildir.

## Açık internet sözlüğü

Geliştirici doğrulama aracında yalnızca örnek **yöntem** kelimesi açık kaynaklara soruldu. Türkçe Vikisözlük ve İngilizce Wiktionary cevap verdi. Ücretli API anahtarı veya hesap kullanılmadı. Seçim/paragraf/Word belgesi gönderilmedi. Gerçek cevap, kaynak URL ve lisans bilgisi `verification.json` içindedir; sonraki erişimin sürekliliği garanti değildir.

Parafraz motoru internetten bağımsızdır. İnternet tamamen kapalı/offline ayarlarında ağ çağrısı yapılmadığı otomatik testte denetlendi.


## Kapsam ve açık kabul işleri

Bu sürüm yerel, deterministik akademik Türkçe parafrazdır. LLM/üretken model çağrısı ve ücretli API anahtarı gerektirmez. Başlangıç verisi 61 kural ve 39 sözlük kaydıdır; genel Türkçe sentaksının tamamını veya her akademik alanı kapsamaz. İngilizce çeviri/parafraz motoru ve kişisel üslup öğrenen model içermez. Güven puanları ölçülmüş doğruluk yüzdesi değildir.

Aşağıdakiler hâlâ gerçek Word bulunan Windows üzerinde doğrulanmalıdır:

- Kurulum/kaldırma, yayıncı güveni ve Word Ribbon/task pane yükleme.
- Word 32/64 bit matrisi, kullanıcı başına yükleme ve upgrade.
- Gerçek font/paragraf/tablo/stil/nesne/dipnot/sonnot/üstbilgi/altbilgi ve seçim dışı içerik koruması.
- Gerçek hyperlink clickability/hedefi, gizli bookmark/REF ve Zotero/Mendeley/EndNote alanları.
- Tek Ctrl+Z, rollback, Track Changes, iptal, kapanış ve UI tepki süresi.
- Geniş akademik metin korpusunda uzman anlam/gramer/üslup değerlendirmesi.

Kurulum ZIP'inde `Verify-Word.cmd` ve `Smoke-Word.ps1` bulunur. Kurulumdan sonra Word kapalıyken CMD çift tıklanarak gerçek Word'de yeni, kaydedilmeyen bir belgeyle başlangıç kabulü çalıştırılabilir. Word sürümü ve işlem mimarisi `Word-Acceptance-Report.json` içinde kaydedilir. Bu başlangıç testi geniş kabul matrisinin tamamı değildir; ayrıntılar `Windows-Acceptance.md` içindedir.

## Sertifika ve kanıtlar

VSTO manifestleri CI'de oluşturulan geliştirme code-signing sertifikasıyla imzalıdır. Açık sertifika `publisher.cer` teslim edilir; özel anahtar/PFX teslim edilmez. Installer EXE için üretim Authenticode sertifikası sağlanmamıştır. Kurulumda Windows/VSTO yayıncı veya güven uyarısı görülebilir.

Windows kanıtları: Debug/Release TRX, VSTO MSBuild binlogları, payload envanteri, `windows-runtime-verification.json`, `installer-prerequisite-verification.json`, gerçek VSTO manifestleri ve installer SHA256. Kaynak/motor arşivinde JSON/TRX/özet kayıtları; ayrı Windows kanıt ZIP'inde tam CI çıktıları bulunur. Kurulum ZIP'indeki EXE, Windows CI'nin SHA256 kaydıyla yerel aktarım sonrası yeniden doğrulanır.
