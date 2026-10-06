# Akademik Parafraz 1.0.2 — Word önbelleğinde SQLite yükleme düzeltmesi

Tanı raporunda 13/13 dosya mevcut, Word dışındaki altı bileşen kontrolü başarılıydı. Word açılışı ise `AP_CONTROLLER_Exception_80131500` ile duruyordu. Bu bulgular kullanıcının yanlış kurulum yaptığı sonucunu desteklemiyor.

## Bulgu ve değişiklik

Gerçek Windows/.NET Framework DLL shadow-copy koşulunda, eski yükleme davranışıyla `DATABASE:FAIL:Exception:80131500` yeniden oluştu. Hatanın ilk method’u `SQLitePCL.NativeLibrary.Load`; rapordaki `assemblyStorage: SHADOW`, DLL’nin gerçekten CLR önbelleğinden yüklendiğini doğruluyor. Bu kullanıcıdaki hata ile tutarlı bir neden; kullanıcının Word’ünde aynı çağrı konumu henüz gözlemlenmedi.

SQLitePCLRaw net461 yükleyicisi native DLL’yi yönetilen DLL’nin yanında veya belirli alt dizinlerde arıyor. Word/CLR yönetilen DLL’leri önbelleğe kopyalayınca `runtimes` klasörü aynı konumda bulunmayabiliyor. Yeni başlangıç kodu, çalışan Word işleminin x86/x64 mimarisine uygun paketlenmiş SQLite DLL’sini doğrulayıp gerçek SQLitePCL yükleme klasörüne atomik olarak hazırlıyor. SHA256 eşleşiyorsa mevcut dosyayı yeniden yazmıyor. Üçüncü taraf DLL veya provider değiştirilmedi.

Java/JAR yolu da önbellekteki `Assembly.Location` yerine doğrulanan gerçek kurulum klasöründen alınıyor. Yeni hata kodları önizleme, veritabanı ve dil motoru gibi açılış aşamalarını ayrı gösteriyor. Yerel tanı kaydı yalnızca kod, süreç mimarisi ve method adı içeriyor; belge metni, exception mesajı, dosya yolu veya tam stack trace içermez.

## Gerçek Windows sonuçları

Windows CI: [37454118096](https://github.com/lolo9723/academicworks/actions/runs/37454118096) — **success**. Derlenen kaynak: `86c496baeeef3c9d1cd08bdf2775c253023ecd01`.

- Tam VSTO Debug/Release derlemesi geçti; gerçek SQLite ve Zemberek motor testleri her yapılandırmada 64/64 geçti.
- Düzeltmesiz gerçek önbellek koşulunda SQLite hatası yeniden oluştu; negatif kontrol başarılıdır.
- Düzeltmeyle aynı önbellek koşulu x64 ve x86 süreçlerde geçti. Doğrudan yükleme de iki mimaride geçti. Dört raporun her birinde yedi bileşen PASS: Framework, native SQLite hazırlığı, gerçek veritabanı, WinForms önizleme, iki sözlük sağlayıcısı ve bundled Java/Zemberek.
- Windows DPAPI geri çözme/bozulmuş veri reddi, native PE mimarileri ve iki VSTO manifestinin gerçek XML imzaları/dosya özetleri geçti.
- Inno Setup gerçek 1.0.2 EXE üretti; Word olmayan Windows’ta gerçek installer erken önkoşul reddi verdi, eklenti kaydı değişmedi.
- İndirilen GitHub parça/evidence ZIP’leri artifact SHA256 özetleriyle, birleştirilen EXE Windows’ta hesaplanan SHA256 ile doğrulandı.

EXE: `AkademikParafraz-1.0.2-Setup.exe` — 39,183,306 byte. SHA256: `af40f28cd67d9b4f822bddd4396f219e90d264d382daac1595da9b3194db685c`.

## Kurulum ve kalan doğrulama

1. Açık belgelerinizi kaydedip bütün Word pencerelerini kapatın.
2. `AkademikParafraz-1.0.2-Windows-kurulum.zip` dosyasını bir klasöre çıkarın.
3. İçindeki `AkademikParafraz-1.0.2-Setup.exe` dosyasını çalıştırın, sonra Word’ü yeniden açın. Önceki sürüm aynı kurulum klasöründe güncellenir; kişisel sözlük/ayar/geçmiş verileri ayrı klasördedir.

Bu oturuma gerçek Word masaüstü bağlı değildir; CI raporlarında `wordExecuted: false` bulunur. Bu nedenle kullanıcının Word’ünde açılışın düzeldiği ve canlı font/link/Undo davranışı henüz doğrulanmış değildir. Kurulumda geliştirme yayıncı sertifikası nedeniyle güven sorusu görülebilir. Açılış hatası sürerse yeni `AP_...` kodu veya paket içindeki `Tanila.cmd` raporu paylaşılmalıdır. Yeni tanılama geçici DLL önbelleğinde çalışır, Word’ü açmaz, kişisel veritabanına yazmaz ve internet sorgusu yapmaz.

Canlı Word kabulü için paket içindeki `Verify-Word.cmd` ayrı bir araçtır. İngilizce çeviri/parafraz ve kişisel üslup öğrenmesi bu Türkçe kural tabanlı sürüme eklenmedi.
