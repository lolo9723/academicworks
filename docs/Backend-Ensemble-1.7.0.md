# 1.7.0: İki kuruluş üreticisi ve bağımsız çerçeve denetimi

15 Ekim 2026. Bu geliştirme, kullanıcının bir paragrafına özel eşleştirme yerine varsayılan motorun genel üretim kapsamını artırır. Kullanıcı paragrafı alınmadı veya sunucuya gönderilmedi.

## Değişiklikler

- UDPipe/Zemberek öbek taşıma üreticisine RuleConstructionBackend eklendi. Mevcut cümle kuruluşları ve isteğe bağlı, sabit özetle doğrulanan internet yapı bankası artık aynı varsayılan akışta aday üretir. Ek üreticide kelime seçimi kapalıdır.
- Her aday özgün Word düzenlenebilir aralıklarına eşlenir. Korunan metnin sayısı/sırası, atıf, sayılar ve birimler denetlenir. Ek üretici Word sınırlarını veya bağımsız anlam denetimini atlamaz.
- ClauseMeaningIntegrity, olgu/rol grafiğini yeniden karşılaştırır. Dar öbek taşıma durumunda bilinen çekim imzası, belirtili nesne ve ad öbeği bağları ayrıca korunur. Çok yüklemli, odak parçacıklı veya belirsiz nesneli taşıma bu ek yoldan onaylanmaz.
- Amaç/hedef cümlesinde sahip, değişmeden kopyalanmış eylem öbeği ve karşılık gelen amaç fiili ayrı eşlenir. Genel fiil kökü eşitliği bu dar dönüşümde tek karar ölçüsü değildir. Eylem, olumsuzluk, hedef/amaç ayrımı, özne ve tamamlanmışlık değişimi kabul edilmez.
- Cümle başındaki “Bu çalışma” gibi yapılarda, bilinen tek kişisiz isim çözümlemesi varsa yanlış olumsuz emir okuması düzeltilir. Noktalama ile ayrılan gerçek emir (“Bu, çalışma!”) korunur. Bu, genel ve kusursuz bir morfolojik belirsizlik giderici değildir.
- Nominative ve PurposeAcc çekimleri gerçek Zemberek üreticisinden gelir. Noun-genitive/iyelik belirsizliğinde, doğrulanmış kişisiz analiz bulunmadan yeni bir ikinci kişi iyeliği üretilmez. Belirsizliği olmayan kişi iyeliği sessizce kaldırılmaz.
- Bağımsız iki yönlü yerel NLI kontrolü ek üretici için zorunludur. NLI tek başına karar vermez. Bu sınıflandırıcı eğitimli bir modeldir; yeni bir büyük üretici dil modeli eklenmedi.
- Sözlük taraması bağlam/tanım odaklıdır: yerel tanım varsa sırf eş anlamlı alternatif yok diye web isteği yapılmaz. HTTP hata kodları ve zaman aşımı ayrı görünür; devre kesici yüzünden atlanan sorgular yeni bağlantı hatası olarak sayılmaz.
- İptalde Java anlam/dil bilgisi işleminin gerçekten çıkışı beklenir. Büyük sentetik girişle gerçek iptal sınaması yapılır.

## Sınırlar ve ölçüm

İlk 1.6.1 Windows koşusunda 207/207 teknik test iki yapılandırmada geçti. Sözlük akışı çağrılmıştı, ancak varsayılan motorun çevrimiçi taramasında 65 köke ait 195 sağlayıcı hata kaydı oluşmuştu; bunların bir bölümü art arda hatalardan sonra atlanan sorgulardı. Buna karşılık ayrı canlı sözlük sınamasında bilgi alınmıştı. Bu fark saklanmaz ve kullanıcının ağına ilişkin bir kanıt sayılmaz.

1.7.0 için iki korpus ayrı tutulur. Önceki beş alan korpusu SHA256 ile değiştirilmeden korunur. İkinci korpus 12 sentetik gelişim paragrafıdır: rekreasyon, psikoloji, tarım, ekonomi, mühendislik, hukuk, tarih, eğitim ve gözlemsel sağlık anlatımları ile İngilizce denetim/rekreasyon. Aynı ikinci korpusta ek üretici açık/kapalı karşılaştırılır. Bu korpus kör insan kabul sınaması değildir ve eğitimde kullanılmaz. Kabul edilen paragraf sayısı, doğal üslup veya anlam doğruluğu yüzdesi değildir.

Tanım ve karmaşık karşıtlık cümleleri dahil kapsam dışında kalan yapılar olabilir. Türkçe dil bilgisi denetimi hâlâ sınırlıdır. Bazı doğru dönüşümler parser veya NLI uyuşmazlığı nedeniyle elenir. Genel serbest yeniden yazım, çeviri, kişisel üslup eğitimi veya internetten sınırsız öğrenme eklenmedi. Gerçek Word ve 8 GB kullanıcı cihazı kabulü henüz yapılmadı.

Windows Debug ve Release yapılandırmalarının her birinde **234/234** teknik test geçti. Aynı ortak korumalara sahip tek üretici, 12 sentetik gelişim metninde **5/12**; iki üreticili yol **8/12** öneri kabul etti. Linux karşılaştırması da 5/12 ve 8/12 oldu. Önceki beş alan korpusunda Windows 3/5, Linux 4/5 olarak kaldı. Bunlar insan kalitesi puanı değildir. Windows ve Linux internet sınamasında 24 kök sorgusu, sıfır sağlayıcı hatası görüldü. Word/işletim sistemi hariç Windows bileşenlerinin tepe belleği yaklaşık 1.579 MiB idi. [Ham sonuçlar, kabul/ret metinleri ve sınırlar](Backend-Ensemble-Results-1.7.0.md). [Windows koşusu](https://github.com/lolo9723/academicworks/actions/runs/38720100423).
