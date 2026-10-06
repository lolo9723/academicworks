# Akademik Parafraz

Bu Windows kurulum paketi geliştirme sürümüdür. Gerçek Word içindeki kurulum, biçim, bağlantı ve geri alma kabulü henüz tamamlanmamıştır. Paket geliştirme yayıncı sertifikası kullanır; kurulumda yayıncı/güven sorusu görülebilir.

## Kurulum

Windows 10/11 x64 ve masaüstü Word 2016 veya daha yeni bir sürüm gerekir. Word 32 veya 64 bit olabilir. Açık belgelerinizi kaydedip Word'ü kapatın ve `AkademikParafraz-1.2.0-Setup.exe` dosyasını açın. Kurulum eksik .NET Framework 4.8, Word veya Microsoft VSTO Runtime bileşenini bildirirse önce o bileşeni tamamlayın. VSTO Runtime'ın resmi indirmesi: https://www.microsoft.com/en-us/download/details.aspx?id=105522

Java, NLP motoru ve sözlük pakete dahildir. Visual Studio, SDK, Maven veya ayrıca Java kurmanız gerekmez. Yayıncı sertifikası otomatik güvenilir yapılmaz; kurumsal bilgisayarınız kurulumu engellerse kurumunuzun yazılım yöneticisiyle ilerleyin.

## Kullanım

1. Word'ü açın ve bir cümle, paragraf veya birkaç paragraf seçin.
2. İnternet desteği için **AKADEMİK PARAFRAZ > İnternetle Parafraz** komutunu kullanın. Bu komut interneti açar, çevrimdışı modu kapatır ve Güçlü düzeye geçer. İnternetsiz çalışmak için Ayarlar’dan interneti kapatıp normal **Parafraz Et** komutunu kullanın.
3. Önizlemede öneriyi okuyun; gerekirse sonraki alternatife geçin.
4. **Uygula** öneriyi belgeye yazar. **İptal** belgeyi değiştirmez.
5. Son işlemi **Ctrl+Z** veya eklentinin **Geri Al** komutuyla geri alın.

Koruma seçenekleri varsayılan olarak açıktır. Bağlantının görünen metnini değiştirmek için **Bağlantıları Koru** seçeneğini kapatın; URL hedefini değiştirmek amaçlanmaz. **Değişiklikleri İzleyerek Parafraz Et** seçeneği Word'ün doğal değişiklik izleme özelliğini kullanır. Korunmasını istediğiniz özel ifadeyi seçip **Terimi Kilitle** komutuyla sözlüğünüze ekleyin.

## İnternet ve kapsam

Parafraz yerelde çalışır; LLM veya ücretli API anahtarı kullanılmaz. Dahili ekran kartı yeterlidir; büyük model indirilmez. 8 GB RAM için ağır model yerine diskte indeksli sözlük kullanılır. Java işleminin azami heap ayarı 512 MB'dır; toplam bellek bunun üzerinde olabilir ve Word belgenizin büyüklüğüne göre değişir.

1.2.0 paketinde KeNet'ten 82.155 farklı madde içeren yerel sözlük ve 1.252 etkin dönüşüm kuralı bulunur. Bu sayı 1.252 bağımsız dilbilgisi fikri demek değildir: yöntem, amaç, neden, edilgen anlatım ve benzeri kalıpların zaman/olumsuzluk biçimleri de ayrı kurallardır. Çok anlamlı bir sözcüğün herhangi bir eş anlamlısı otomatik seçilmez. Akademik terimler, kilitli ifadeler, sayılar ve atıflar korunduğunda değişim miktarı sınırlanabilir. Kişisel sözlük ve değiştirdiğiniz kurallar güncellemede korunur.

İnternet yeni kurulumda normal komut için kapalıdır. **İnternetle Parafraz** açıldıktan sonra sonraki işlemlerde bu ayar korunur; kapatmak için Ayarlar’ı kullanın. 1.2.0 tüm uygun, benzersiz kök/tür çiftlerini inceler; 200 kök ve üç internet kelimesi sınırları kaldırılmıştır. Yerel karşılık bulunmayanların tamamı iki internet sözlük kaynağında aranır. Sözlüklerde bilgi yoksa Wikidata’da Türkçe tam eşleşen terim kaydı denenir; bu bilgi eş anlamlı veya otomatik değiştirme karşılığı sayılmaz. Wikidata açıklaması başka dilde olabilir. Sayılar, özel isimler ve kilitli/korunan alanlar bu taramadan çıkarılır; çekimli sözcükler kök/tür ile aranır. İnternet taraması uzun sürebilir; ilerleme panelde gösterilir ve İptal belgeyi değiştirmeden durdurur. Her isteğin süre/boyut sınırı vardır; art arda üç erişim hatası veren kaynak beş dakika bekletilir, diğer kaynak devam eder. Olumsuz arama sonucu bir saat, bulunan sözlük verisi 30 gün önbellekte tutulur.

Yalnızca kelime kökü gönderilir; paragraf yüklenmez. Vikisözlük/Wiktionary ve Wikidata’nın ücretsiz, anahtar istemeyen MediaWiki arayüzü kullanılır; teknik olarak bu da bir API'dir, ücretli hesap gerektirmez. Çok anlamlı karşılıklar ve dil/tür uyumsuzluğu reddedilir. Yerelde hiç olmayan bir sözcüğün otomatik karşılığı için internet kaynağındaki tek anlam/tür ve iki yönlü açık eş anlamlı ilişkisi aranır; son çekim denetimini Zemberek yapar. Her kelimenin bulunacağı veya değiştirileceği garanti edilmez. Önizleme bulunan/bulunamayan kökleri ve kaynak hatalarını sayar.

İnternet yapı bankası 25.104 birleşim içerir: edilgen raporlama, çalışma/araştırma bağlamı, özne/yüklem sırası, zaman/olumsuzluk ve amaç kalıpları. Bunlar 25.104 bağımsız dilbilgisi ailesi değildir. Banka projenin sürümlü, denetlenmiş JSON kaynağından HTTPS ile alınır ve SHA256 ile doğrulanır; bu isteğe belge veya kelime eklenmez. Seçime uygun kalıplar gerektiğinde oluşturulur; binlerce regex aynı anda belleğe yüklenmez. İndirilen banka sonraki çevrimdışı işlemde de kullanılabilir. Yeni bir yerel tam cümle kalıbı yoksa bankanın ek kalıbı işe yarayabilir; banka da yapıyı kapsamıyorsa uygulama rastgele web cümlesinden anlam eşdeğerliği öğrenmez. Arama motoru sonuçlarını otomatik kurala dönüştürme ve kendiliğinden sınırsız dil öğrenme yoktur.

Bu sürüm akademik Türkçe içindir; İngilizce çeviri/parafraz ve kişisel üslup öğrenmesi içermez. Sözlük, cümle bağlamını eksiksiz anlayan bir çeviri motoru değildir. Güvenli öneri bulunmayan ifadeler değişmeden kalır. Her önerinin anlamını uygulamadan önce okuyun; sayısal bir anlam doğruluğu yüzdesi gösterilmez.

## Word doğrulaması

Kurulumdan sonra Word'ü kapatıp kurulum ZIP'indeki `Verify-Word.cmd` dosyasını açabilirsiniz. Test yeni, kaydedilmeyen bir Word belgesi kullanır; atıf, istatistik, teknik terim, font/paragraf biçimi, hyperlink hedefi, tek Undo ve Track Changes kontrollerini çalıştırır. Ayarlarınıza geri döner ve `Word-Acceptance-Report.json` dosyasını oluşturur. Açık Word belgelerini kendiliğinden kapatmaz. Bu sonuç, geniş Word 32/64 bit kabul matrisinin tamamı değildir.

Eklenti normal Windows **Yüklü Uygulamalar** ekranından kaldırılır. Kişisel sözlük, ayar ve geçmiş verileri `%LOCALAPPDATA%\AkademikParafraz` altında saklanır.

## Açılış hatası tanılaması

1.2.0 sürümü Word’ün DLL önbelleğine uygun SQLite yüklemesini hazırlar ve Java/dil motorunu gerçek kurulum klasöründen bulur. Açılış hatası sürerse mesajdaki `AP_...` hata kodunu paylaşın. Paket içindeki `Tanila.cmd`, kurulu bileşenleri gerçek .NET Framework altında kontrol eder ve aynı klasörde `AkademikParafraz-Tani.json` oluşturur.

Tanılama; Word’e benzer .NET Framework DLL önbelleğinde yükleme, SQLite başlangıcı, dosya varlığı, geçici test veritabanı, önizleme kontrolü, geniş sözlük verisinin okunması, sözlük sağlayıcılarının oluşturulması ve bundled Java/Zemberek ile örnek kelime analizi içerir. Word'ü açmaz, mevcut kişisel sözlüğe/geçmişe yazmaz ve internet sorgusu yapmaz. Raporda belge metni, exception mesajı, stack trace veya kullanıcı adı bulunmaz. Gerçek Word kabulü için ayrı `Verify-Word.cmd` kullanılır.
