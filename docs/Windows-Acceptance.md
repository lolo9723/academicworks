# Windows / Microsoft Word yayın kabulü

**Durum: Windows derleme, motor testleri ve installer üretimi geçti; canlı Word kabulü çalıştırılmadı.** Windows CI kanıtları doğrulama raporundadır. Word içindeki kabulü derleme sonuçlarıyla karıştırmayın. Yayın için aşağıdaki canlı kontrollerin sonuçları Word build numarası, Windows sürümü, Office bitness, kullanılan sertifika ve installer SHA256 ile kaydedilmelidir.

## Derleme ve kurulum

- [x] Visual Studio 2022 OfficeTools ve .NET 4.8 Developer Pack bulunan Windows makinesinde Build-Windows.ps1 tamamlandı.
- [x] Debug/Release motor testleri gerçek bundled Windows Java ile geçti.
- [ ] WordAddin.dll, dll.manifest ve .vsto üretildi, imzaları geçerli.
- [x] Payload içinde managed NuGet bağımlılıkları, SQLite x86/x64, Zemberek JAR ve Java runtime/legal dosyaları var.
- [x] Inno Setup 6.3+ gerçek Setup.exe oluşturdu; örnek dosyayla installer üretimi kabul sayılmadı.
- [ ] Temiz Windows + Word 32 bit üzerinde kullanıcı başına kuruldu; AKADEMİK PARAFRAZ sekmesi göründü.
- [ ] Temiz Windows + Word 64 bit üzerinde kuruldu; native SQLite doğru bitness ile yüklendi.
- [ ] Yönetici olmayan kullanıcı kurulumu çalıştı; yayıncı/VSTO güven politikası açıkça kaydedildi.
- [ ] Word kapalı değilken installer düzgün açıklama verdi, açık belgeyi zorla kapatmadı.
- [ ] Eksik .NET/Word/VSTO Runtime kontrolleri doğru hata verdi; başarısız VSTO imza/güven adımı başarı gösterilmedi.
- [ ] Standart kaldırma eklenti kaydını/kurulum dosyalarını temizledi, Word açılabildi; kullanıcı DB/yedekleri korundu.
- [ ] Upgrade kişisel kuralları, sözlüğü, ayarları ve DPAPI geçmişi korudu.

## Otomatik gerçek Word testi

Kurulmuş eklenti bulunan test makinesinde açık Word belgelerini kaydedip Word'ü kapatın. Kaynak kökünden:

```powershell
.\build\Smoke-Word.ps1
```

Betik Word'ü açar, yeni ve kaydedilmemiş bir belge oluşturur, gerçek eklentinin COM automation nesnesini çağırır; şartnamedeki cümleyi Orta düzeyde dönüştürür. Atıf/istatistik/teknik terim, başka paragraf, font/italik/paragraf hizası ve tek Undo kontrol edilir; ardından gerçek revisions üretimi ve link koruması açık/kapalı durumda hyperlink görünen metni/URL kontrol edilir. Belge kaydedilmeden kapanır. Word sürümü/mimarisi ve sonuçlar `Word-Acceptance-Report.json` dosyasına yazılır. `Verify-Word.cmd` aynı testi çift tıklamayla başlatır. Bu betiğin PASS çıkması aşağıdaki geniş Word matrisi yerine geçmez. Test makinesi varsayılan kuralları kullanmalı; özel kurallar/ayarlar kaydedilip korunmalıdır.

- [ ] Smoke-Word.ps1 gerçek Word'de PASS verdi; konsol çıktısı ve test makinesi bilgileri saklandı.

## Biçim ve yapı

Her denemede önce/sonra metin, ilgili OOXML ve URL hedefleri karşılaştırılmalı; yalnızca seçimin onaylanan edit aralıkları değişmelidir. Normal Word kayıt işlemlerinin rsid/metadata değişiklikleri, eklentinin paragraf/nesne değişiklikleriyle karıştırılmamalıdır.

- [ ] Aynı cümlede normal/kalın/italik/altı çizili/üst-alt simge/renkli farklı run'lar: biçim sınırını geçen kalıp atlandı, güvenli küçük bölümler doğru değişti.
- [ ] Font, punto, iki yana yaslama, sağ/sol/ilk satır girintileri, önce/sonra boşluklar ve satır aralığı korundu.
- [ ] Başlık stilleri, numaralandırma ve madde işaretleri korundu.
- [ ] Aynı biçimde birkaç run'a bölünmüş kalıp güvenli değişti; başka run biçimleri etkilenmedi.
- [ ] Tek hücre, birkaç hücre, birleştirilmiş hücreler: hücre sonları/tablolar/gölgelendirme/genişlik/kenarlık korundu veya güvenli reddedildi.
- [ ] Resim/şekil/grafik/SmartArt/denklem aynı konumda kaldı; nesne metinleri düzenlenmedi.
- [ ] Page/section break ve satır sonu bulunan seçim güvenli işlendi veya açık mesajla reddedildi.
- [ ] Seçim dışındaki paragraflar ve belgenin diğer story'leri değişmedi.

## Alanlar, bağlantılar ve atıflar

- [ ] Varsayılan hyperlink koruması görünen metin ve URL'yi korudu.
- [ ] Koruma kapalıyken yalnızca linkin görünen metni değişebildi; URL/SubAddress/clickability korundu.
- [ ] Basit w:hyperlink, fldSimple ve complex HYPERLINK alanları ayrı denendi.
- [ ] Link dışı/içi aynı biçimde olsa bile sınır aşan bir kalıp uygulanmadı.
- [ ] Bookmark, REF/cross-reference, caption numarası ve kaynakça/citation field kod/sonuçları korundu.
- [ ] Gerçek Zotero, Mendeley ve EndNote belgelerinde selection field'ın içinden başladığında dahi koruma çalıştı.
- [ ] APA, Vancouver, anlatısal atıf, çoklu atıf, DOI, URL, ISBN/ISSN/PMID ve istatistik örnekleri değişmedi.
- [ ] Özel regex ve kilitli teknik terimler, ekli/yumuşamış örnekler korunarak çalıştı.

## Story ve geri alma

- [ ] Ana metin seçimi çalıştı.
- [ ] Gerçek dipnot/sonnot seçimi, marker ile/markersız ve çoklu note durumları denendi; güvenli eşlenemeyen alan reddedildi.
- [ ] Gerçek üstbilgi/altbilgi seçimi denendi; farklı bölüm/header parçası doğru bulundu veya güvenli reddedildi.
- [ ] Tek Ctrl+Z bütün parafraz editlerini eski metin/biçime döndürdü.
- [ ] Eklenti Geri Al çalıştı; seçim dışında başka kullanıcı düzenlemesi sonrası yanlış son işlemi geri almadı.
- [ ] Track Changes kapalı/açık, mevcut revisions olan belge, korunan fields ile birlikte denendi.
- [ ] Kullanıcının eski TrackRevisions ayarı işlemden sonra korundu.
- [ ] Hatalı bir edit denemesi tek Undo ile rollback oldu, kısmi bozuk belge bırakmadı.

## Kullanıcı akışları / gizlilik / hız

- [ ] 1 cümle, 1 paragraf ve 10 paragrafta UI responsive kaldı; ilk NLP başlatma ile sıcak işlem süreleri ayrı ölçüldü.
- [ ] Uzun işlem iptali belgeyi değiştirmedi; sonraki işlem NLP'yi yeniden açabildi.
- [ ] Word kapanışı sırasında bekleyen işlem UI/COM exception veya arka planda Java süreci bırakmadı.
- [ ] Boş seçim, yalnızca resim, salt okunur/protected belge ve eksik NLP bileşeni düzgün mesaj verdi.
- [ ] Hafif/Orta/Güçlü, gerçek farklı alternatifler, sonraki/önceki ve Apply/Cancel çalıştı.
- [ ] Ayar/sözlük/kurallar/terim kilidi/yedek import-export tanılama ekranları çalıştı.
- [ ] Ağ kapalı/offline modda parafraz sırasında hiç HTTP isteği yoktu.
- [ ] İsteğe bağlı sözlükte yalnızca yazılan tek kelime dışarı gitti; provider kesintisi parafrazı durdurmadı.
- [ ] SQL history blob'ları DPAPI ile şifreli; uygulama loglarında seçilmiş akademik metin yok.
- [ ] Alan uzmanı, kuralları farklı akademik metinlerde anlam/olumsuzluk/kip/çatı/özne uyumu için değerlendirdi; güven puanı başarı oranı olarak raporlanmadı.

Bütün bu kayıtlar olmadan “üretim kalitesinde, Windows'ta eksiksiz doğrulanmış” sonucu verilmemelidir. Derleme/test/installer üretimi Windows CI ile doğrulanmıştır; Word içindeki kabul kutuları canlı Word çalıştırılmadan işaretlenmez.
