# Akademik Parafraz

Bu Windows kurulum paketi geliştirme sürümüdür. Gerçek Word içindeki kurulum, biçim, bağlantı ve geri alma kabulü henüz tamamlanmamıştır. Paket geliştirme yayıncı sertifikası kullanır; kurulumda yayıncı/güven sorusu görülebilir.

## Kurulum

Windows 10/11 x64 ve masaüstü Word 2016 veya daha yeni bir sürüm gerekir. Word 32 veya 64 bit olabilir. Açık belgelerinizi kaydedip Word'ü kapatın ve `AkademikParafraz-1.1.0-Setup.exe` dosyasını açın. Kurulum eksik .NET Framework 4.8, Word veya Microsoft VSTO Runtime bileşenini bildirirse önce o bileşeni tamamlayın. VSTO Runtime'ın resmi indirmesi: https://www.microsoft.com/en-us/download/details.aspx?id=105522

Java, NLP motoru ve sözlük pakete dahildir. Visual Studio, SDK, Maven veya ayrıca Java kurmanız gerekmez. Yayıncı sertifikası otomatik güvenilir yapılmaz; kurumsal bilgisayarınız kurulumu engellerse kurumunuzun yazılım yöneticisiyle ilerleyin.

## Kullanım

1. Word'ü açın ve bir cümle, paragraf veya birkaç paragraf seçin.
2. **AKADEMİK PARAFRAZ > Güçlü > Parafraz Et** komutunu kullanın. Hafif daha çok kelime düzeyinde değişir; Orta ve Güçlü cümle kalıplarını da kullanır.
3. Önizlemede öneriyi okuyun; gerekirse sonraki alternatife geçin.
4. **Uygula** öneriyi belgeye yazar. **İptal** belgeyi değiştirmez.
5. Son işlemi **Ctrl+Z** veya eklentinin **Geri Al** komutuyla geri alın.

Koruma seçenekleri varsayılan olarak açıktır. Bağlantının görünen metnini değiştirmek için **Bağlantıları Koru** seçeneğini kapatın; URL hedefini değiştirmek amaçlanmaz. **Değişiklikleri İzleyerek Parafraz Et** seçeneği Word'ün doğal değişiklik izleme özelliğini kullanır. Korunmasını istediğiniz özel ifadeyi seçip **Terimi Kilitle** komutuyla sözlüğünüze ekleyin.

## İnternet ve kapsam

Parafraz yerelde çalışır; LLM veya ücretli API anahtarı kullanılmaz. Dahili ekran kartı yeterlidir; büyük model indirilmez. 8 GB RAM için ağır model yerine diskte indeksli sözlük kullanılır. Java işleminin azami heap ayarı 512 MB'dır; toplam bellek bunun üzerinde olabilir ve Word belgenizin büyüklüğüne göre değişir.

1.1.0 paketinde KeNet'ten 82.155 farklı madde içeren yerel sözlük ve 1.252 etkin dönüşüm kuralı bulunur. Bu sayı 1.252 bağımsız dilbilgisi fikri demek değildir: yöntem, amaç, neden, edilgen anlatım ve benzeri kalıpların zaman/olumsuzluk biçimleri de ayrı kurallardır. Çok anlamlı bir sözcüğün herhangi bir eş anlamlısı otomatik seçilmez. Akademik terimler, kilitli ifadeler, sayılar ve atıflar korunduğunda değişim miktarı sınırlanabilir. Kişisel sözlük ve değiştirdiğiniz kurallar güncellemede korunur.

İnternet varsayılan olarak kapalıdır. Ayarlardan interneti açıp çevrimdışı modu kapatırsanız parafraz sırasında en fazla üç farklı sözcüğün kökü için, toplam sekiz saniye bütçesiyle sözlük desteği denenir. Yalnızca kelime gönderilir; paragraf yüklenmez. Sözlük penceresinde de tek kelime arayabilirsiniz. Vikisözlük/Wiktionary'nin ücretsiz, anahtar istemeyen MediaWiki arayüzü kullanılır; teknik olarak bu da bir API'dir, ücretli hesap gerektirmez. Eş anlamlı otomatik uygulanmadan önce yerel sözlükle uyumu kontrol edilir. Kaynak erişilemiyorsa yerel motor devam eder; her kelimede internetten sonuç bulunacağı garanti edilmez.

Bu sürüm akademik Türkçe içindir; İngilizce çeviri/parafraz ve kişisel üslup öğrenmesi içermez. Sözlük, cümle bağlamını eksiksiz anlayan bir çeviri motoru değildir. Güvenli öneri bulunmayan ifadeler değişmeden kalır. Her önerinin anlamını uygulamadan önce okuyun; sayısal bir anlam doğruluğu yüzdesi gösterilmez.

## Word doğrulaması

Kurulumdan sonra Word'ü kapatıp kurulum ZIP'indeki `Verify-Word.cmd` dosyasını açabilirsiniz. Test yeni, kaydedilmeyen bir Word belgesi kullanır; atıf, istatistik, teknik terim, font/paragraf biçimi, hyperlink hedefi, tek Undo ve Track Changes kontrollerini çalıştırır. Ayarlarınıza geri döner ve `Word-Acceptance-Report.json` dosyasını oluşturur. Açık Word belgelerini kendiliğinden kapatmaz. Bu sonuç, geniş Word 32/64 bit kabul matrisinin tamamı değildir.

Eklenti normal Windows **Yüklü Uygulamalar** ekranından kaldırılır. Kişisel sözlük, ayar ve geçmiş verileri `%LOCALAPPDATA%\AkademikParafraz` altında saklanır.

## Açılış hatası tanılaması

1.1.0 sürümü Word’ün DLL önbelleğine uygun SQLite yüklemesini hazırlar ve Java/dil motorunu gerçek kurulum klasöründen bulur. Açılış hatası sürerse mesajdaki `AP_...` hata kodunu paylaşın. Paket içindeki `Tanila.cmd`, kurulu bileşenleri gerçek .NET Framework altında kontrol eder ve aynı klasörde `AkademikParafraz-Tani.json` oluşturur.

Tanılama; Word’e benzer .NET Framework DLL önbelleğinde yükleme, SQLite başlangıcı, dosya varlığı, geçici test veritabanı, önizleme kontrolü, geniş sözlük verisinin okunması, sözlük sağlayıcılarının oluşturulması ve bundled Java/Zemberek ile örnek kelime analizi içerir. Word'ü açmaz, mevcut kişisel sözlüğe/geçmişe yazmaz ve internet sorgusu yapmaz. Raporda belge metni, exception mesajı, stack trace veya kullanıcı adı bulunmaz. Gerçek Word kabulü için ayrı `Verify-Word.cmd` kullanılır.
