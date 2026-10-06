# Akademik Parafraz 1.0.1 — açılış düzeltmesi

Kullanıcı 1.0.0 paketinde “Akademik Parafraz başlatılamadı. Kurulumu onarın” mesajı bildirdi. Bu mesaj, ThisAddIn.Initialize içindeki genel catch bloğuydu; gerçek exception nedenini kaydetmiyordu. Kullanıcının yanlış kurulum yaptığı sonucunu destekleyen bir hata kodu yoktu.

Panel koleksiyonu BeginInit/EndInit ile hazırlanacak şekilde değiştirildi. Controller ve panel oluşturma, Initialize içinde erken Add çağırmak yerine VSTO Startup olayına taşındı. Ribbon controller bağlandığında invalidate edilir; başlangıçta önbellekte kalan disabled durum güncellenir.

Açılış hatası artık AP_<aşama>_<exception tipi>_<HRESULT> koduyla gösterilir. Yerel startup-diagnostics.txt yalnızca zaman, kod ve süreç mimarisi kaydeder; exception mesajı/stack trace/belge metni kaydetmez. Tanila.cmd, gerçek .NET Framework altında kurulu dosyaları ve bağımsız bileşenleri inceler; Word açmaz, kullanıcı DB'sine yazmaz, internet sorgusu yapmaz. NLP kontrolü bundled Java/Zemberek ile sabit bir örnek kelime kullanır.

Windows CI: [37441471927](https://github.com/lolo9723/academicworks/actions/runs/37441471927) — success. Derlenen commit: `9709d70c7ac1c507e900bfd5fec8123959c1f61e`.

- Tam VSTO Debug/Release derlemesi geçti; motor testleri her iki yapılandırmada 64/64, sıfır başarısız/atlanan.
- Kurulu paket düzeni ve gerçek ürün DLL/config dosyalarıyla .NET Framework x64 ve x86 açılış kontrolleri geçti: framework yükleme, gerçek SQLite migration/61 kural/39 sözlük kaydı/ayar okuma, WinForms önizleme, iki sözlük sağlayıcısının oluşturulması ve gerçek bundled Java/Zemberek analizi. Bu kontroller Word COM'unu çalıştırmadı.
- Windows CurrentUser DPAPI roundtrip/değiştirilmiş veri reddi, native PE mimarileri, VSTO XML imzaları ve manifest dosya hashleri geçti.
- Gerçek Inno Setup 1.0.1 EXE üretildi; eksik Word önkoşulu gerçek EXE üzerinde exit 1 ile reddedildi, eklenti kaydı değişmedi.
- GitHub'dan alınan parça/evidence ZIP SHA256 özetleri doğrulandı; parçalar birleştirildikten sonra EXE Windows'ta hesaplanan SHA256 ile birebir eşleşti.

EXE: `AkademikParafraz-1.0.1-Setup.exe` — 39,184,796 byte. SHA256: `42d7d7fffa4fe976d4f383a8fcc4377b6de8707434352949b7fc214cb7def38c`.

Kanıtlar `startup-verification.json` ve `startup-x86-verification.json` içinde `passed: true`, `wordExecuted: false` olarak kayıtlıdır. Sertifika geliştirme yayıncı sertifikasıdır; bu paket üretim Authenticode sertifikası kullanmaz.

Bu oturuma Word masaüstü bağlı değildir. VSTO başlangıç sırasındaki eksiklik kaynakta düzeltilmiştir; kullanıcının gördüğü hatanın tek nedeni olduğu ve kendi Word ortamında giderildiği henüz doğrulanmış değildir. Yeni kod veya tanılama raporu ile kalan nedenler ayırt edilebilir. Gerçek Word font/link/Undo kabulünün yerine bileşen kontrolü konmaz.

Kurulum: açık belgeleri kaydedip Word'ü kapatın, 1.0.1 ZIP'ini çıkarın ve AkademikParafraz-1.0.1-Setup.exe dosyasını çalıştırın. Word'ü yeniden açın. Açılış hatası sürerse yeni AP_... kodunu veya Tanila.cmd'nin ürettiği AkademikParafraz-Tani.json raporunu paylaşın. Word kabul testi ayrı Verify-Word.cmd dosyasındadır.
