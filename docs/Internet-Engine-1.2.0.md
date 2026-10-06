# Akademik Parafraz 1.2.0 — İnternet taraması ve cümle yapı bankası

1.2.0, eksik kelimelerin tamamını internet kaynaklarında arayan ve denetlenmiş ek yapı bankasını internetten indiren Word komutunu ekler. LLM, ücretli API anahtarı veya büyük model kullanılmaz. Dahili GPU yeterlidir. 8 GB RAM gözetilerek indeksli sözlük ve sınırlı Java heap kullanılır; bütün Word belgelerinde hız/bellek garantisi verilmez.

## Gerçek kapsam

- KeNet: 82,155 farklı madde, 78,327 anlam kümesi, 110,259 üyelik. Veriler CC BY-SA 4.0; upstream GPL kodu kullanılmadı. Kaynak commit ve SHA256 doğrulandı. İndeksli verinin boyutu 19,394,560 byte.
- 61 temel ve 1.191 ek kural: toplam 1.252. Çekim/olumsuzluk/yöntem/amaç/neden kalıplarının biçimleri ayrı sayılır; bu sayı bağımsız sentaks fikirlerinin sayısı değildir.
- Sözcük türü/çekim, anlam kümesi ve akademik kayıt filtresi geçmeyen sözlük karşılığı otomatik değiştirilmez. 82.155 maddenin tamamı otomatik eş anlamlı kapsamı değildir.
- İnternet açık, offline kapalıysa bütün uygun benzersiz kök/tür çiftleri incelenir; yerel karşılığı eksik kalanlar için üç kelime/sekiz saniye ve 200 kök sınırları kaldırıldı. Tek isteğin süre/boyut sınırı, kaynak hatasında bekletme ve kullanıcı iptali vardır; paragraf gönderilmez. Sözlükler boşsa tam Türkçe eşleşme ile Wikidata terim bilgisi denenir, açıklama eş anlamlı sayılmaz. Ücretsiz MediaWiki arayüzü teknik olarak API'dir, hesap/anahtar gerektirmez. Kaynak kesintisinde yerel motor devam eder.
- Bir cümle dönüşümünün korunan sayı/terim çevresindeki editleri birlikte seçilir. Word eşlemesi editlerden birine izin vermiyorsa tüm alternatif reddedilir; önizleme dışı kısmi metin uygulanmaz.

## Doğrulanmış Windows sonuçları

Windows CI [37468777249](https://github.com/lolo9723/academicworks/actions/runs/37468777249) başarılı. Derlenen kaynak `e98d717edc70135f74f266dc34dca48a4de3c3bb`.

Debug ve Release gerçek SQLite/Zemberek testleri 106/106 geçti. Tam VSTO projeleri derlendi; iki manifestin XML imzası/dosya özetleri, DPAPI geri çözme/bozulmuş veri reddi ve eksik-Word installer önkoşulu geçti. .NET Framework doğrudan ve gerçek DLL önbelleğinden yüklemede x86/x64, dört ayrı süreçte dokuz bileşen PASS verdi. KeNet dosyası da her süreçte gerçekten okundu. Eski SQLite yükleme davranışının negatif kontrolünde beklenen 80131500 hatası yeniden oluştu.

Sentetik altı cümlelik testte ilk öneride 6 metin düzenlemesi; on paragrafta 60 düzenleme üretildi. Soğuk ilk paragraf 1555 ms, sıcak on paragraf 245 ms. Ölçülen .NET motoru + gözlemlenen Java süreçleri yaklaşık 365 MiB çalışma belleği kullandı. **Word işlemi ölçülmedi; CI donanımı kullanıcının 8 GB bilgisayarı değildir.** Java heap sınırı 512 MiB, toplam Java belleği heap sınırından farklıdır. Bu örnek genel anlam doğruluğu testi veya kişisel üslup öğrenmesi değildir.

## Canlı internet sonucu ve kapsamı

Gerçek internet denemesinde anahtar kullanılmadı, paragraf gönderilmedi. Windows raporunda sözlük/terim kaynağı erişimi: True. Beş sentetik örnek kökün tamamı incelendi, 5/5 kök için bilgi bulundu. Bulunan terim bilgisi, beş otomatik eş anlamlı dönüşüm demek değildir. Test örnekleri yöntem, örneklem, parafraz, ontoloji ve metottur. Yerel geliştirme makinesinde de bu beşinin bilgisi bulundu; parafraz için Wikidata CC0 kaydı kullanıldı.

Yapı bankası 25.104 birleşim içerir; 25.104 bağımsız dilbilgisi ailesi değildir. Banka kaynağı: `https://raw.githubusercontent.com/lolo9723/academicworks/main/data/internet-structures-v1.json`. Wire SHA256: `84d9115d8b36128fa6950bc15fb464e8c359699f4f0b2abab3fe991eb5fe56c7`. Gerçek HTTP indirmesi bu özetle doğrulandı. Yalnızca seçime uygun kalıplar oluşturulur; bütün envanter için regex nesneleri açılmaz. Banka açık bir sürümlü proje kaynağıdır; arama motorlarından rastgele cümle toplama veya sınırsız kendiliğinden dil öğrenme yoktur. Yerel kalıp yetmediğinde banka ek kalıp sağlayabilir; bankada da olmayan yapı dönüştürülmeyebilir.

Gerçek HTTP ile yeni yapı örneği:

Mevcut incelemede yanıtlar analiz edilmemiştir (Alfa, 2022; N=120).

→ Yanıtlar mevcut incelemede çözümlenmemiştir (Alfa, 2022; N=120).

Olumsuzluk, atıf ve istatistik değişmedi. Kaynak kesintisi, yanlış SHA256, aşırı yanıt boyutu, iptal, offline cache, 200 kök sınırının kaldırılması, sekiz eksik kökün tamamının sorgulanması, iki yönlü eş anlamlı doğrulaması ve yalnızca tam Türkçe Wikidata eşleşmesi test edildi. Her kelimenin bulunması veya her cümlenin tamamen yeniden yazılması garanti edilmez.

## Gerçek yerel çıktı örneği

Kaynak (sentetik):

İçerik analizi, ziyaretçi deneyimlerini incelemenin en etkili yöntemlerinden biridir. Ziyaretçi deneyimlerini anlamayı mümkün kıldığı için bu araştırmada içerik analizi kullanılmıştır. Ziyaretçilerin çevrimiçi yorumları incelenmiştir. Araştırma alanı olarak 12 destinasyon belirlenmiştir. Bu yorumlarda deneyimlerin nasıl temsil edildiği analiz edilmiştir. Bu araştırmanın amacı, ziyaretçi deneyimlerini incelemektir (Alfa, 2022; N=120).

Motorun ilk önerisi:

İçerik analizi, ziyaretçi deneyimlerini incelemenin en etkili yöntemleri arasında yer almaktadır. Ziyaretçi deneyimlerini anlamaya olanak sağladığından bu araştırmada içerik analizi kullanılmıştır. Ziyaretçilerin çevrimiçi yorumları incelemeye tabi tutulmuştur. Araştırma alanı olarak 12 destinasyon saptanmıştır. Bu yorumlarda deneyimlerin temsil edilme biçimleri çözümlenmiştir. Bu araştırma, ziyaretçi deneyimlerini incelemeyi amaçlamaktadır (Alfa, 2022; N=120).

## Kurulum

Açık belgelerinizi kaydedip Word'ü kapatın. Kurulum ZIP'ini çıkarıp `AkademikParafraz-1.2.0-Setup.exe` dosyasını açın. Aynı ürün klasörünü günceller; kişisel ayar/sözlük/geçmiş farklı klasördedir. Önceki kişisel kuralın üzerine yazılmaması motor testinde kontrol edildi; gerçek Word yükseltmesi ayrıca doğrulanmalıdır. Word'de paragraf seçip **İnternetle Parafraz** komutunu kullanın. Bu komut interneti açıp Güçlü düzeye geçer; ayar sonraki işlemlerde korunur. Önizlemede öneriyi ve bulunamayan kök sayısını okuyun, sonra Uygula'yı kullanın. İptal uzun taramayı belgeye yazmadan durdurur.

EXE 43,833,745 byte; SHA256 `fd364e10501cafc3665b8d373541a57b41ca39d7cbe573aba6853c1781960038`. İndirilen GitHub artifact ZIP özetleri, CRC ve birleştirilmiş EXE'nin Windows'ta hesaplanan SHA256 değeri doğrulandı.

## Kalan sınırlar

Bu oturuma canlı masaüstü Word bağlı değildir. Raporda `wordExecuted: false` korunmuştur. Word biçimi, tıklanabilir bağlantı hedefi, Ctrl+Z ve Track Changes davranışı gerçek Word'de `Verify-Word.cmd` ile ayrıca test edilmelidir. Geliştirme yayıncı sertifikası kullanılır. İngilizce çeviri/parafraz ve kişisel üslup öğrenmesi bu sürümde yoktur. Kusursuz veya bütün metinleri tamamen değiştiren bir motor olarak sunulmaz; öneri anlamını kullanıcı gözden geçirmelidir.
