# Akademik Parafraz 1.1.0 — geniş sözlük ve cümle dönüşümleri

Yerel Türkçe motor, tek kelime değişimiyle sınırlı örnekler için genişletildi. LLM, ücretli API anahtarı veya büyük model kullanılmaz. Dahili GPU yeterlidir. 8 GB RAM gözetilerek indeksli sözlük ve sınırlı Java heap kullanılır; bütün Word belgelerinde hız/bellek garantisi verilmez.

## Gerçek kapsam

- KeNet: 82,155 farklı madde, 78,327 anlam kümesi, 110,259 üyelik. Veriler CC BY-SA 4.0; upstream GPL kodu kullanılmadı. Kaynak commit ve SHA256 doğrulandı. İndeksli verinin boyutu 19,394,560 byte.
- 61 temel ve 1.191 ek kural: toplam 1.252. Çekim/olumsuzluk/yöntem/amaç/neden kalıplarının biçimleri ayrı sayılır; bu sayı bağımsız sentaks fikirlerinin sayısı değildir.
- Sözcük türü/çekim, anlam kümesi ve akademik kayıt filtresi geçmeyen sözlük karşılığı otomatik değiştirilmez. 82.155 maddenin tamamı otomatik eş anlamlı kapsamı değildir.
- İnternet açık, offline kapalıysa en fazla üç farklı kök sözcük için sekiz saniye bütçesiyle açık sözlük desteği denenir; paragraf gönderilmez. Ücretsiz MediaWiki arayüzü teknik olarak API'dir, hesap/anahtar gerektirmez. Kaynak kesintisinde yerel motor devam eder.
- Bir cümle dönüşümünün korunan sayı/terim çevresindeki editleri birlikte seçilir. Word eşlemesi editlerden birine izin vermiyorsa tüm alternatif reddedilir; önizleme dışı kısmi metin uygulanmaz.

## Doğrulanmış Windows sonuçları

Windows CI [37464862615](https://github.com/lolo9723/academicworks/actions/runs/37464862615) başarılı. Derlenen kaynak `bc55e07d194b4aec372c16a4131aa3baf8caa393`.

Debug ve Release gerçek SQLite/Zemberek testleri 89/89 geçti. Tam VSTO projeleri derlendi; iki manifestin XML imzası/dosya özetleri, DPAPI geri çözme/bozulmuş veri reddi ve eksik-Word installer önkoşulu geçti. .NET Framework doğrudan ve gerçek DLL önbelleğinden yüklemede x86/x64, dört ayrı süreçte sekiz bileşen PASS verdi. KeNet dosyası da her süreçte gerçekten okundu. Eski SQLite yükleme davranışının negatif kontrolünde beklenen 80131500 hatası yeniden oluştu.

Sentetik altı cümlelik testte ilk öneride 6 metin düzenlemesi; on paragrafta 60 düzenleme üretildi. Soğuk ilk paragraf 1515 ms, sıcak on paragraf 275 ms. Ölçülen .NET motoru + gözlemlenen Java süreçleri yaklaşık 408 MiB çalışma belleği kullandı. **Word işlemi ölçülmedi; CI donanımı kullanıcının 8 GB bilgisayarı değildir.** Java heap sınırı 512 MiB, toplam Java belleği heap sınırından farklıdır. Bu örnek genel anlam doğruluğu testi veya kişisel üslup öğrenmesi değildir.

## Gerçek çıktı örneği

Kaynak (sentetik):

İçerik analizi, ziyaretçi deneyimlerini incelemenin en etkili yöntemlerinden biridir. Ziyaretçi deneyimlerini anlamayı mümkün kıldığı için bu araştırmada içerik analizi kullanılmıştır. Ziyaretçilerin çevrimiçi yorumları incelenmiştir. Araştırma alanı olarak 12 destinasyon belirlenmiştir. Bu yorumlarda deneyimlerin nasıl temsil edildiği analiz edilmiştir. Bu araştırmanın amacı, ziyaretçi deneyimlerini incelemektir (Alfa, 2022; N=120).

Motorun ilk önerisi:

İçerik analizi, ziyaretçi deneyimlerini incelemenin en etkili yöntemleri arasında yer almaktadır. Ziyaretçi deneyimlerini anlamaya olanak sağladığından bu araştırmada içerik analizi kullanılmıştır. Ziyaretçilerin çevrimiçi yorumları incelemeye tabi tutulmuştur. Araştırma alanı olarak 12 destinasyon saptanmıştır. Bu yorumlarda deneyimlerin temsil edilme biçimleri çözümlenmiştir. Bu araştırma, ziyaretçi deneyimlerini incelemeyi amaçlamaktadır (Alfa, 2022; N=120).

## Kurulum

Açık belgelerinizi kaydedip Word'ü kapatın. Kurulum ZIP'ini çıkarıp `AkademikParafraz-1.1.0-Setup.exe` dosyasını açın. Aynı ürün klasörünü günceller; kişisel ayar/sözlük/geçmiş farklı klasördedir. Önceki kişisel kuralın üzerine yazılmaması motor testinde kontrol edildi; gerçek Word yükseltmesi ayrıca doğrulanmalıdır. Güçlü modda bir paragraf seçip öneriyi okuyun, sonra Uygula'yı kullanın.

EXE 43,819,572 byte; SHA256 `7f8e9f80459b31ee33eb1ff6895c0dba6377ddf864f8bcdf97280a79aa1c155d`. İndirilen GitHub artifact ZIP özetleri, CRC ve birleştirilmiş EXE'nin Windows'ta hesaplanan SHA256 değeri doğrulandı.

## Kalan sınırlar

Bu oturuma canlı masaüstü Word bağlı değildir. Raporda `wordExecuted: false` korunmuştur. Word biçimi, tıklanabilir bağlantı hedefi, Ctrl+Z ve Track Changes davranışı gerçek Word'de `Verify-Word.cmd` ile ayrıca test edilmelidir. Geliştirme yayıncı sertifikası kullanılır. İngilizce çeviri/parafraz ve kişisel üslup öğrenmesi bu sürümde yoktur. Kusursuz veya bütün metinleri tamamen değiştiren bir motor olarak sunulmaz; öneri anlamını kullanıcı gözden geçirmelidir.
