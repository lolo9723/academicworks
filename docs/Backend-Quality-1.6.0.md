# Akademik Parafraz 1.6.0 — gerçek sonuçlar ve Windows paketi

Kaynak: `14a8b43ebce8904c0dbd46c14d567007684e014d`. [Windows koşusu](https://github.com/lolo9723/academicworks/actions/runs/37945782385).

**Deneysel geliştirme sürümü. İstenen genel, doğal ve kapsamlı parafraz kalitesi henüz karşılanmadı.** Kelime eş anlamlılarıyla değiştirme yerine ayrı çözümleme, kuruluş, anlam, dil bilgisi ve nihai değerlendirme bileşenleri çalışır. Desteklenen kuruluş işlemleri sınırlıdır; çoğu kabul edilen paragrafta yalnızca bir cümle yeniden sıralanmıştır.

Aynı sabit beş paragrafta Linux 4/5, Windows 3/5 otomatik öneri kabul etti. Eğitim parametreleri ve veri sürümleri aynı olsa da platformda üretilen çözümleyici dosyalarının özetleri/çıktıları farklıdır. İngilizce çevre paragrafındaki öneri Linux üzerinde kabul edilirken Windows üzerinde nesne/yan cümle kapsamı denetiminde elendi. Başarı sayılarını artırmak için denetim gevşetilmedi.

Windows Debug ve Release: her birinde 200/200 teknik test geçti. VSTO derlemesi, x86/x64 doğrudan ve önbellekten Framework açılışı, 11 native dosyanın mimarisi, DPAPI, manifestlerin kriptografik imzaları/dosya özetleri ve eksik Word için kurulum engeli doğrulandı. Teknik testler insan dil kalitesi ölçümü değildir.

**Gerçek Word açılmadı.** Biçim, bağlantılar, Undo ve Track Changes canlı kabulü için paketteki Verify-Word.cmd Word kurulu Windows bilgisayarda çalıştırılabilir. Geliştirme yayıncı sertifikası kullanılır; EXE üretim Authenticode sertifikasıyla imzalanmış değildir.

Hukuk kaynağındaki “başvuruyu süresi içinde yapılması koşuluyla” anlatım sorunu öneride kalmıştır. Dil bilgisi aşamasının geçmesi bu hatanın düzeltildiğini göstermez; Türkçe dil bilgisi denetimi sınırlıdır. Mühendislik paragrafında çözümleyici/çekim uyuşmazlıkları nedeniyle öneri elenmiştir. Bu eksikler, otomatik kabul sayısının neden kalite başarısı olarak sunulamayacağını gösterir.

Ücretli API anahtarı ve ekran kartı gerekmez. Yeni çok aşamalı motor küçük eğitimli çözümleyicileri ve yaklaşık 355 MB önceden eğitilmiş NLI modeli/tokenizer dosyalarını pakette taşır; ayrıca 2,5 GB üretici model indirmesi gerekmez. NLI ağırlıkları bu projede yeniden eğitilmedi. Paragraf sunucuya gönderilmez. Sınamalar sentetik/kamusal metinlerle yapıldı; kullanıcının diş hekimliği ve beslenme örnekleri eğitim veya beş alan testi için kullanılmadı.

Çeviri, kişisel üslup öğrenimi, bütün cümlelerin serbest yeniden yazımı, sınırsız internetten yapı öğrenimi ve kusursuz anlam korunumu sağlanmış değildir. Nihai öneriler önizlemede okunmalıdır.

EXE SHA256: `3d5159e8c14d48f6a0a025cf3971cb2866401d1408280e8acc5b5c75e57aad4b`.

Sabit test korpusu SHA256: `a38b41287a8af00026c5e3b201f4eaf2f74019da7a0bcc129fa6a84e8a9c7187`. Aynı beş alan korpusu değiştirilmedi ve eğitime eklenmedi.

## Windows: gerçek backend sınaması

Beş farklı alandaki paragrafın 3/5 tanesinde otomatik kabul edilen öneri oluştu. Bu sayı insan değerlendirmesi, anlam doğruluğu veya bütün paragrafın yeniden yazıldığı anlamına gelmez.

Bağımsız olgu/rol denetimi: anlamı değiştirilmiş sekiz çiftte 0/8 yanlış kabul; anlamı koruyan dört çiftte 3/4 yanlış ret. NLI tek başına: 2/8 yanlış kabul, 1/4 yanlış ret.

İptal çözümleyici sürecini kapattı: True; anlam sürecini kapattı: True.

En yüksek sahip olunan uygulama + çözümleyici + dil bilgisi + NLI çalışma belleği: 1566 MiB. Word ve işletim sistemi dahil değildir; gerçek 8 GB kullanıcı makinesi ölçümü değildir.

### hukuk

Kaynak: Kurul, başvuruyu süresi içinde yapılması koşuluyla değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

Otomatik kabul edilen öneri: Başvuruyu süresi içinde yapılması koşuluyla kurul değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

Değiştirilen cümle sayıları: [1].
Süre: 8.8 saniye.

Kabul edilen deneme: Başvuruyu süresi içinde yapılması koşuluyla kurul değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede ana yüklem değişti.
- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9830; geri=0.9602; çelişki=0.0011/0.0033.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

### muhendislik

Kaynak: Deney sırasında sensörün kaydettiği sıcaklık 42 °C olarak ölçülmüştür. Bu ölçüm, parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını göstermemektedir.

Kabul edilebilir öneri üretilmedi.

Değiştirilen cümle sayıları: [].
Süre: 4.2 saniye.

Elenen deneme: Deney sırasında sensörün kaydettiği sıcaklık 42 °C olarak ölçülmüştür. Parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını bu ölçüm göstermemektedir.

- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: kalacağını
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9738; geri=0.9815; çelişki=0.0009/0.0005.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

### tarih

Kaynak: Arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Otomatik kabul edilen öneri: Dönemin ticari ilişkileri hakkında arşivde bulunan mektuplar bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Değiştirilen cümle sayıları: [1].
Süre: 5.7 saniye.

Kabul edilen deneme: Dönemin ticari ilişkileri hakkında arşivde bulunan mektuplar bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9455; geri=0.9706; çelişki=0.0017/0.0021.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

Elenen deneme: Bilgi arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli: Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.8868; geri=0.9149; çelişki=0.0074/0.0034.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

### egitim

Kaynak: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Araştırmacılar, bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini belirtmiştir.

Otomatik kabul edilen öneri: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

Değiştirilen cümle sayıları: [1].
Süre: 7.3 saniye.

Elenen deneme: Dersin değerlendirme yönteminin açık olduğunu öğrencilerden alınan geri bildirimler göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: olduğunu
- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9834; geri=0.9958; çelişki=0.0009/0.0002.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9934; geri=0.9813; çelişki=0.0010/0.0006.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

Elenen deneme: Dersin değerlendirme yönteminin açık olduğunu öğrencilerden alınan geri bildirimler göstermiştir. Araştırmacılar, bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini belirtmiştir.

- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: olduğunu
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9834; geri=0.9958; çelişki=0.0009/0.0002.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

Kabul edilen deneme: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9934; geri=0.9813; çelişki=0.0010/0.0006.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

### environment

Kaynak: The survey identified an association between tree cover and lower reported heat exposure. Because the data were observational, the authors did not claim that planting trees caused the difference.

Kabul edilebilir öneri üretilmedi.

Değiştirilen cümle sayıları: [].
Süre: 8.0 saniye.

Elenen deneme: The survey identified an association between tree cover and lower reported heat exposure. The authors did not claim that planting trees caused the difference because the data were observational.

- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: observational
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9552; geri=0.9756; çelişki=0.0024/0.0012.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

### Eğitimden ayrı çözümleyici testleri

Aşağıdaki sonuçlar altın tokenizasyonla etiketleme/ağaç başarısıdır; anlam veya parafraz doğruluğu değildir. Raporun elapsedSeconds alanı önceden eğitilmiş dosyaları yeniden kullanırken hazırlama/test süresini de gösterebilir; eğitim süresi olarak yorumlanmaz.

tr: UD_Turkish-BOUN; bu projede eğitildi: True; model SHA256: `4495c85a437fabd93c8088f9bc5d3c66f144cfdae846cc658d460cb4fcfb9beb`.

```text
Loading UDPipe model: done.
Tagging from gold tokenization - forms: 12210, upostag: 91.41%, xpostag: 87.28%, feats: 79.34%, alltags: 70.76%, lemmas: 88.84%
Parsing from gold tokenization with computed tags - forms: 12210, UAS: 67.44%, LAS: 58.38%
```

en: UD_English-EWT; bu projede eğitildi: True; model SHA256: `3a3e118b5c7ad0dbc35c783c98236fe8e0d7df70e67305015c1144e0873c0e05`.

```text
Loading UDPipe model: done.
Tagging from gold tokenization - forms: 25094, upostag: 94.88%, xpostag: 93.94%, feats: 95.15%, alltags: 92.31%, lemmas: 96.10%
Parsing from gold tokenization with computed tags - forms: 25094, UAS: 83.64%, LAS: 80.34%
```

## Linux: gerçek backend sınaması

Beş farklı alandaki paragrafın 4/5 tanesinde otomatik kabul edilen öneri oluştu. Bu sayı insan değerlendirmesi, anlam doğruluğu veya bütün paragrafın yeniden yazıldığı anlamına gelmez.

Bağımsız olgu/rol denetimi: anlamı değiştirilmiş sekiz çiftte 0/8 yanlış kabul; anlamı koruyan dört çiftte 2/4 yanlış ret. NLI tek başına: 2/8 yanlış kabul, 1/4 yanlış ret.

İptal çözümleyici sürecini kapattı: True; anlam sürecini kapattı: True.

En yüksek sahip olunan uygulama + çözümleyici + dil bilgisi + NLI çalışma belleği: 1861 MiB. Word ve işletim sistemi dahil değildir; gerçek 8 GB kullanıcı makinesi ölçümü değildir.

### hukuk

Kaynak: Kurul, başvuruyu süresi içinde yapılması koşuluyla değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

Otomatik kabul edilen öneri: Başvuruyu süresi içinde yapılması koşuluyla kurul değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

Değiştirilen cümle sayıları: [1].
Süre: 14.1 saniye.

Kabul edilen deneme: Başvuruyu süresi içinde yapılması koşuluyla kurul değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede ana yüklem değişti.
- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9830; geri=0.9601; çelişki=0.0011/0.0033.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

### muhendislik

Kaynak: Deney sırasında sensörün kaydettiği sıcaklık 42 °C olarak ölçülmüştür. Bu ölçüm, parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını göstermemektedir.

Kabul edilebilir öneri üretilmedi.

Değiştirilen cümle sayıları: [].
Süre: 12.2 saniye.

Elenen deneme: Deney sırasında sensörün kaydettiği sıcaklık 42 °C olarak ölçülmüştür. Parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını bu ölçüm göstermemektedir.

- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: kalacağını
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9725; geri=0.9872; çelişki=0.0009/0.0006.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

### tarih

Kaynak: Arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Otomatik kabul edilen öneri: Dönemin ticari ilişkileri hakkında arşivde bulunan mektuplar bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Değiştirilen cümle sayıları: [1].
Süre: 14.3 saniye.

Elenen deneme: Dönemin ticari ilişkileri hakkında arşivde bulunan mektuplar bilgi sağlamaktadır. Ancak o olayın hiç yaşanmadığını belgelerde bir olayın yer almaması kanıtlamaz.

- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede ana yüklem değişti.
- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: yaşanmadığını
- meaning-and-derivation: Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.
- meaning-and-derivation: Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli: Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9452; geri=0.9706; çelişki=0.0017/0.0021.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.8649; geri=0.8799; çelişki=0.0033/0.0103.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

Elenen deneme: Bilgi arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında sağlamaktadır. Ancak o olayın hiç yaşanmadığını belgelerde bir olayın yer almaması kanıtlamaz.

- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede ana yüklem değişti.
- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: yaşanmadığını
- meaning-and-derivation: Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.
- meaning-and-derivation: Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli: Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9789; geri=0.9149; çelişki=0.0010/0.0034.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.8649; geri=0.8799; çelişki=0.0033/0.0103.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

Kabul edilen deneme: Dönemin ticari ilişkileri hakkında arşivde bulunan mektuplar bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9452; geri=0.9706; çelişki=0.0017/0.0021.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

Elenen deneme: Bilgi arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9789; geri=0.9149; çelişki=0.0010/0.0034.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

Elenen deneme: Arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında bilgi sağlamaktadır. Ancak o olayın hiç yaşanmadığını belgelerde bir olayın yer almaması kanıtlamaz.

- meaning-and-derivation: Yeniden çözümlemede ana yüklem değişti.
- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: yaşanmadığını
- meaning-and-derivation: Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.
- meaning-and-derivation: Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli: Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.8649; geri=0.8799; çelişki=0.0033/0.0103.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

### egitim

Kaynak: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Araştırmacılar, bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini belirtmiştir.

Otomatik kabul edilen öneri: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

Değiştirilen cümle sayıları: [1].
Süre: 7.2 saniye.

Elenen deneme: Dersin değerlendirme yönteminin açık olduğunu öğrencilerden alınan geri bildirimler göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: olduğunu
- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: gerektiğini
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9670; geri=0.9958; çelişki=0.0025/0.0002.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9934; geri=0.9840; çelişki=0.0010/0.0007.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

Elenen deneme: Dersin değerlendirme yönteminin açık olduğunu öğrencilerden alınan geri bildirimler göstermiştir. Araştırmacılar, bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini belirtmiştir.

- meaning-and-derivation: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: olduğunu
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9670; geri=0.9958; çelişki=0.0025/0.0002.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

Kabul edilen deneme: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation notu: Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: gerektiğini
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9934; geri=0.9840; çelişki=0.0010/0.0007.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

### environment

Kaynak: The survey identified an association between tree cover and lower reported heat exposure. Because the data were observational, the authors did not claim that planting trees caused the difference.

Otomatik kabul edilen öneri: An association between tree cover and lower reported heat exposure was identified by the survey. Because the data were observational, the authors did not claim that planting trees caused the difference.

Değiştirilen cümle sayıları: [1].
Süre: 9.7 saniye.

Elenen deneme: An association between tree cover and lower reported heat exposure was identified by the survey. The authors did not claim that planting trees caused the difference because the data were observational.

- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: observational
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: caused
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9928; geri=0.9889; çelişki=0.0006/0.0010.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9552; geri=0.9429; çelişki=0.0024/0.0036.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

Kabul edilen deneme: An association between tree cover and lower reported heat exposure was identified by the survey. Because the data were observational, the authors did not claim that planting trees caused the difference.

- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9928; geri=0.9889; çelişki=0.0006/0.0010.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

Elenen deneme: The survey identified an association between tree cover and lower reported heat exposure. The authors did not claim that planting trees caused the difference because the data were observational.

- meaning-and-derivation: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: observational
- meaning-and-derivation: Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: caused
- meaning-and-derivation notu: Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.
- meaning-and-derivation notu: Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.
- bidirectional-local-nli notu: Model desteği (doğruluk yüzdesi değildir): ileri=0.9552; geri=0.9429; çelişki=0.0024/0.0036.
- bidirectional-local-nli notu: NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.

### Eğitimden ayrı çözümleyici testleri

Aşağıdaki sonuçlar altın tokenizasyonla etiketleme/ağaç başarısıdır; anlam veya parafraz doğruluğu değildir. Raporun elapsedSeconds alanı önceden eğitilmiş dosyaları yeniden kullanırken hazırlama/test süresini de gösterebilir; eğitim süresi olarak yorumlanmaz.

tr: UD_Turkish-BOUN; bu projede eğitildi: True; model SHA256: `5577b0650a2a748d3e52a2c1f602da8dd953850fd715541d4506fc382e049473`.

```text
Loading UDPipe model: done.
Tagging from gold tokenization - forms: 12210, upostag: 91.41%, xpostag: 87.28%, feats: 79.34%, alltags: 70.76%, lemmas: 88.84%
Parsing from gold tokenization with computed tags - forms: 12210, UAS: 67.40%, LAS: 58.35%
```

en: UD_English-EWT; bu projede eğitildi: True; model SHA256: `63f06bf2f1100471f596d1c05cc97f4c1969fba0ef337cc873705836ec32c94f`.

```text
Loading UDPipe model: done.
Tagging from gold tokenization - forms: 25094, upostag: 94.88%, xpostag: 93.94%, feats: 95.15%, alltags: 92.31%, lemmas: 96.10%
Parsing from gold tokenization with computed tags - forms: 25094, UAS: 83.45%, LAS: 80.23%
```
