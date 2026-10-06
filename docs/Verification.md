# Akademik Parafraz — gerçek doğrulama raporu

Tarih: 2026-10-05T21:33:48.7574131Z

Çalıştırılan ortam: Debian GNU/Linux 13 (trixie). .NET SDK 8.0.425, gerçek Java 17, Zemberek 0.17.1. Kütüphane/ürün kaynak sürümü 1.0.0. Bu rapor **Windows'ta kurulmuş ve üretim kabulünden geçmiş bir Word ürünü belgesi değildir**.

## Gerçekten çalıştırılan kontroller

`AcademicParaphraser.Engine.sln` restore edildi. Core, Infrastructure, .NET Framework 4.8 WordHost, test ve doğrulama projeleri Debug ve Release derlendi: sıfır compiler warning, sıfır error. WordHost'taki COM automation köprüsü de .NET Framework reference assemblies ile derlendi; Word COM çağrısı çalıştırılmadı.

Java sidecar Maven ile derlendi ve testlerde gerçek süreç olarak otomatik başlatıldı. Testler gerçek SQLite dosyaları ve Zemberek analizi/çekim üretimi kullandı. Testlerde NLP mock'u veya sahte dönüştürme cevabı yoktur.

| Yapılandırma | Toplam | Geçti | Başarısız | Atlandı |
| --- | ---: | ---: | ---: | ---: |
| Debug | 64 | 64 | 0 | 0 |
| Release | 64 | 64 | 0 | 0 |

Kontroller; APA/Vancouver/anlatısal atıflar, DOI/URL/ISBN/ISSN/PMID, istatistikler/sayılar, kilitli terim ve yumuşama, gerçek Türkçe ek üretimi ve belirsizliği bırakma, büyük harf/noktalama, farklı düzey/alternatifler, bağımsız paragraf bağlamı, 10 paragraf, run/biçim/link sınırları, Zotero field, tablo sonları ve özellikleri, story paketleri, inline nesne işaretleri/şekil metni, DTD, migration, atomik yedek, NaN/null reddi, şifreli veri ve metinsiz log/offline davranışını kapsar. OOXML kontrolleri gerçek Word UI/COM testinin yerine geçmez.

Şifreleme testlerinde cross-platform AES-CBC/HMAC kullanıldı. Ana ürünün Windows DPAPI sağlayıcısı uygulanmış olsa da Windows üzerinde çalıştırılmadı.

Başlangıç verisi: **61 kural, 39 sözlük kaydı**. Bunlar sınırlı akademik kalıplardır; genel Türkçe sentaksını veya her akademik alanı bütünüyle kapsamıyor.

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

## Bağımlılık ve betik kontrolü

NuGet'in güncel advisory kaynağıyla bütün motor/host/test projelerinin transitif paketleri sorgulandı; kayıtlı açık bildirilmedi. İlk denetimde SQLitePCLRaw 2.1.6 için bildirilen açık, bağımlılığı 2.1.13'e yükselterek giderildi. Bu sonuç yalnızca sorgulanan NuGet advisory kaynağının kapsamıdır; tam güvenlik denetimi değildir.

Zemberek'in eski Guava/Protobuf/Caffeine transitif sürümleri sırasıyla 33.4.8-jre / 3.25.8 / 2.9.3 ile değiştirildi ve gerçek Zemberek regresyonları yeniden geçti. Java dependency ağacı ve üçüncü taraf lisans/metadata envanteri pakete eklendi. Java bağımlılıklarına ayrı bir tam CVE taraması çalıştırıldığı iddia edilmez.

Build-Windows.ps1 ve Smoke-Word.ps1, PowerShell'in gerçek parser'ıyla sözdizimi kontrolünden geçti. Windows komutlarının çalıştırıldığı veya installer'ın derlendiği anlamına gelmez. PowerShell ve Inno kaynakları Türkçe karakterler için UTF-8 BOM ile saklanır.

Uygulama kaynaklarında TODO/FIXME/NotImplementedException/placeholder/mock ve LLM servis çağrıları için tarama yapıldı; uygulama kodunda bulunmadı. Build araçlarının ürettiği obj/placeholder metadata'sı uygulama kodu değildir ve kaynak arşivine alınmadı.

## Tamamlanamayan Windows doğrulaması

Tam `AcademicParaphraser.sln` restore edildi; tam derleme **MSB4019** ile başarısız oldu: Linux SDK'da `Microsoft.VisualStudio.Tools.Office.targets` yok. OfficeTools hedefi taklit edilerek sahte VSTO başarı sonucu üretilmedi.

Bu nedenle aşağıdakiler henüz doğrulanmamıştır:

- WordAddin başlangıç projesinin tam VSTO derlemesi, uygulama/deployment manifestlerinin üretim ve imza kontrolü.
- Gerçek Setup.exe/MSI üretimi ve Windows kurulum/kaldırması.
- Gerçek Word'de Ribbon/task pane yükleme; font/paragraf/field/link/dipnot/sonnot ve nesne koruması.
- Gerçek Word'de tek Ctrl+Z, rollback, Track Changes ve COM/UI responsive davranışı.
- Windows Office 32/64 bit matrisi ve DPAPI çalışma davranışı.
- Geniş akademik metin korpusunda uzman tarafından anlam/gramer/üslup değerlendirmesi.

Kaynak ve derlenmiş motor paketi **doğrudan kurulabilir Word ürünü değildir**. Bu sürüm kusursuz parafraz veya eksiksiz belge koruma garantisi olarak sunulmuyor. İngilizce çeviri/parafraz motoru da içermez; son şartnameye göre akademik Türkçe kapsamındadır.

## Kanıt dosyaları

Arşivin `verification` dizininde Debug.trx, Release.trx, build-test.log, wordhost-debug.log, vsto-linux-build.log, powershell-parser.txt, nuget-audit.txt, maven-dependencies.txt ve verification.json bulunur. Kaynakta README, Developer.md, Windows-Acceptance.md, Build-Windows.ps1, Smoke-Word.ps1 ve installer tanımı vardır. Gerçek Windows kabulü tamamlanana kadar Windows-Acceptance.md kutuları çalıştırılmamış olarak kalır.

## Devam çalışması: gerçek VSTO SDK kaynak kontrolü

Microsoft Visual Studio 2022 release kataloğundaki VSTO BuildTools VSIX paketi indirildi, resmi SHA256 özeti doğrulandı. WordAddin başlangıç/Ribbon C# kodu proje dosyasındaki gerçek referanslar ile derlendi. Eksik `Microsoft.Office.Tools.Common.v4.0.Utilities` referansı eklendi; WordHost ile Word/Office interop gömme ayarları uyumlu hale getirildi. Yanlış OfficeTools bileşen kimliği yerine resmi TeamOffice/TeamOffice.BuildTools kullanıldı. Bu kaynak/API kontrolü tam VSTO MSBuild, manifest, installer veya canlı Word kabulü değildir. Kanıt `artifacts/sdk-compile-check/<Configuration>/evidence.json` içindedir.

Windows GitHub Actions akışı eklendi. Gerçek Windows sonucu oluşmadan başarılı derleme/installer üretimi olarak raporlanmaz.
