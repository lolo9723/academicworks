# Akademik Parafraz 1.3.0 — Cümle kuruluşu ve klinik terim koruması

Bu sürümde varsayılan parafraz, otomatik sözlük eş anlamlılarından ayrıldı. Yeni kurulumda ve eski ayarlar yükseltildiğinde `EnableWordChoice=false` olur. KeNet ve internet kaynakları kelime bilgisi toplar; bu bilgiler varsayılan parafrazda kelime değişimi olarak uygulanmaz. Kullanıcı özellikle isterse Ayarlar’daki ayrı kelime önerisi seçeneğini açabilir. İnternetten alınan denetlenmiş bütün-cümle kalıpları kendi kayıtlı hedefleriyle çalışır.

## Yapılan değişiklik

- 303 yeni amaç, ikincil/birincil/temel amaç, yöntem, edilgen raporlama, eşgüdümlü işlem ve cümle sırası biçimi eklendi. Amaç cümlelerinde nesne hâli değiştirilmeden fiilin amaç anlatımına uygun biçimi kullanılır. Olumsuzluk ve zaman yalnızca kayıtlı biçimlerle korunur. Bu sayı 303 bağımsız dilbilgisi ailesi değildir.
- Toplam 1.555 tanımlı kural vardır; varsayılan parafraz kelime/ifade değiştirme kurallarını filtreler. Kullanıcı tarafından özellikle yazılmış kurallar korunur. Geniş sözlük 82.155 KeNet maddesi olarak aynıdır; yeni sözlük sağlayıcısı eklenmedi. Kişisel/teknik başlangıç kayıtları 39’dan 77’ye çıktı; konsültasyon, diş hekimliği, bölüm adları, klinik roller ve diğer teknik terimler koruma amaçlı eklendi. Kurum/bölüm adları için ayrıca ad koruması vardır.
- Önce bütün-cümle dönüşümleri seçilir; bir alternatif, aynı noktadaki cümle dönüşümünü tek kelime önerisine düşüremez. Alternatiflerin yakınlığı yalnızca kelime kümesine göre ölçülmez; kelime sırası da değerlendirilir. En yüksek cümle kapsamına sahip öneriler gösterilir.
- Önizleme artık kaç cümlede yapısal dönüşüm olduğunu gösterir. Bu sayı anlam doğruluğu veya insani kalite puanı değildir. Uygun kalıp yoksa açıkça belirtilir.
- Korunan alanların aynı sırayla ve aynı metinle kalması, editlerin birlikte kabul edilmesi ve Word’ün biçim/alan sınırları gözetilir. Kurum adı cümlenin başında korunuyorsa eklenmesi gereken giriş cümlesi için yalnızca düzenlenebilir boşluk kullanılır; korunan metin değiştirilmez. Uygun boşluk yoksa başka güvenli öneri seçilir.

## Gerçek Windows doğrulaması

Koşu [37475192677](https://github.com/lolo9723/academicworks/actions/runs/37475192677) başarılı. Derlenen kaynak `a5660617b7cefe2e8ea25753505cb38b3d0f39a8`.

Debug ve Release yapılandırmalarında gerçek SQLite, Zemberek ve KeNet ile **122/122 test geçti**; atlanan test yok. Bunların 16’sı yeni varsayılan davranışı, klinik terimleri, üç cümlelik klinik paragrafı, amaçların farklı alanlarda nesne hâlini, olumsuzluğu, zaman biçimlerini, yalnızca sıra değişen alternatifleri ve korunan bağlantı/atıf çevresindeki cümle dönüşümünü denetler. Eski kelime değiştirme testleri artık bu özelliği açıkça açar; varsayılan davranış diye sunulmaz.

Tam Framework/VSTO projeleri derlendi. İki XML manifest imzası/dosya özetleri, Windows DPAPI round-trip/bozulmuş veri reddi, eksik-Word kurulum önkoşulu geçti. Doğrudan ve gerçek DLL önbelleğinden x86/x64 dört ayrı süreçte dokuz bileşen PASS verdi. Eski SQLite hatasının negatif kontrolünde beklenen 80131500 hatası oluştu.

## Motorun klinik örneği

Aşağıdaki kaynak **yapay test metnidir**. Kullanıcının gönderdiği paragraf kamuya açık depoya veya Windows CI’a eklenmedi; yalnızca yerel, Git tarafından dışlanan dosyada denendi.

Kaynak:

Bu araştırmanın amacı, hasta dosyalarını kullanarak Ortodonti ve Periodontoloji bölümleri arasındaki iletişimi incelemektir. Araştırmanın ikincil amacı ise konsültasyon yanıtlarını uzman ve lisansüstü öğrenci gruplarına göre karşılaştırmaktır. Tıp fakültesi arşivindeki 286 hastaya ait konsültasyon kayıtları retrospektif olarak incelenmiş ve kategorize edilmiştir.

Gerçek motor çıktısı:

Bu araştırma, hasta dosyalarını kullanarak Ortodonti ve Periodontoloji bölümleri arasındaki iletişimi incelemeyi amaçlamaktadır. İkincil amaç olarak, araştırma konsültasyon yanıtlarını uzman ve lisansüstü öğrenci gruplarına göre karşılaştırmayı hedeflemektedir. Tıp fakültesi arşivindeki 286 hastaya ait konsültasyon kayıtları üzerinde retrospektif inceleme yapılmış ve kayıtlar kategorilere ayrılmıştır.

Üç alternatifin her birinde 3/3 cümlede yapısal dönüşüm vardır. Ortodonti/Periodontoloji, Tıp fakültesi, konsültasyon ve 286 sayısı korunmuştur; lemma/eş anlamlı editleri yoktur. Bu otomatik kontroller, bütün Türkçe metinlerde eksiksiz anlam eşdeğerliği veya genel kalite kanıtı değildir.

## Başka alan ve internet kontrolü

Altı cümlelik turizm örneğinde ilk öneride 4/6 cümlede yapısal değişim vardır. Bu örneğin iki cümlesi değişmeden kalır; motorun genel kapsam sınırı görünür biçimde korunur. Atıf ve 12 destinasyon ifadesi değişmez. On paragrafın sınırları da korunur. İlk paragraf 1518 ms, sıcak on paragraf 272 ms sürdü. Ölçülen .NET + gözlemlenen Java süreçleri yaklaşık 375 MiB çalışma belleği kullandı. Word belleği ölçülmedi; CI donanımı kullanıcının 8 GB bilgisayarı değildir. Java heap sınırı 512 MiB; GPU gerekmez.

Canlı internet kontrolünde beş kök incelendi, 5/5 kök için bilgi bulundu. Sözlük bilgisi kelime değiştirme başarısı demek değildir. Paragraf gönderilmedi ve ücretli API anahtarı kullanılmadı. SHA256 doğrulanan 25.104 birleşimli yapı bankası gerçekten indirildi; bu envanter bağımsız dilbilgisi fikirlerinin sayısı değildir. İnternetten rastgele cümle toplanarak otomatik kural öğrenilmez. Anahtarsız MediaWiki arayüzleri teknik olarak API’dir.

Canlı alınan yapı örneği:

Mevcut incelemede yanıtlar analiz edilmemiştir (Alfa, 2022; N=120).

→ Yanıtlar mevcut incelemede çözümlenmemiştir (Alfa, 2022; N=120).

## Kurulum

Word belgelerini kaydedip Word’ü kapatın. ZIP’i çıkarıp `AkademikParafraz-1.3.0-Setup.exe` dosyasını açın. Kişisel sözlük/ayar/geçmiş saklanır. Word’de paragrafı seçip **Güçlü > Parafraz Et** veya **İnternetle Parafraz** komutunu kullanın. Ayarlar’daki **Eş anlamlı kelime değişimlerini ayrıca uygula** kapalı kalsın. Önizlemeyi okuyup Uygula’ya basın.

EXE 43,828,300 byte, SHA256 `1b2f4c76ea6e3a8bf66cbc40b9d8bc084846bc771c36773da9834fc3cabe626d`. İndirilen GitHub artifact ZIP özetleri, CRC ve birleştirilmiş EXE’nin Windows’ta hesaplanan özeti doğrulandı. Paket geliştirme yayıncı sertifikası kullanır.

## Kapsam sınırı

Bu sürüm kural temellidir; her tür paragrafı serbestçe, kişisel üslupta yeniden yazan bir dil modeli içermez. Sözlük veya kural sayısını büyütmek böyle bir modelin yerini tutmaz. İngilizce çeviri/parafraz da bu sürümde yoktur. Önceki dil-modeli kullanılmaması tercihi değiştirilmeden model etkinleştirilmedi. Genel ve doğal paragraf yeniden yazımı için ayrı bir yerel model aşaması gerekir; bu aşama tamamlanmış gibi sunulmaz.

Bu oturuma canlı masaüstü Word bağlı değildir; `wordExecuted:false` korunur. Biçim, tıklanabilir bağlantı hedefi, tek Ctrl+Z ve Track Changes gerçek Word’de `Verify-Word.cmd` ile ayrıca kontrol edilmelidir. Bu test betiği de varsayılan cümle dönüştürme davranışına güncellendi. Kusursuz parafraz veya bütün biçimleri koruduğu iddia edilmez.
