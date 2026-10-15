# Akademik Parafraz 1.7.0 — gerçek doğrulama sonuçları

Kaynak: `1dc9e806e0ce3f58291e39f0f611ea6024546f94`. [Windows koşusu](https://github.com/lolo9723/academicworks/actions/runs/38720100423).

**Deneysel sürüm. Genel, doğal ve kapsamlı parafraz hedefi henüz karşılanmadı.** İki bağımsız kuruluş üreticisi aynı Word, olgu, dil bilgisi ve iki yönlü NLI denetiminden geçer. Kelime değişimlerini artırmak kalite hedefi olarak kullanılmaz. Bu sürüm yeni bir büyük üretici model veya yeni bir parser eğitimi içermez.

Windows Debug ve Release yapılandırmalarının her birinde 234/234 teknik test geçti. VSTO manifestleri, 11 native dosya mimarisi, DPAPI, Framework doğrudan/önbellek açılışı (x86/x64), eksik Word için kurulum engeli ve kurulum SHA256 üretimi doğrulandı. Gerçek Word kurulumu/biçim/bağlantı/Undo kabulü ve gerçek 8 GB kullanıcı cihazı ölçümü yapılmadı. Geliştirme yayıncı sertifikası kullanılır.

Sınamalar kullanıcının özel paragrafıyla yapılmadı. Önceki beş alan korpusu değişmeden kaldı. İkinci 12 metinlik korpus sentetik gelişim sınamasıdır; kör insan değerlendirmesi değildir. Aynı ortak korumalarla ek üretici açık/kapalı karşılaştırılır. İkinci korpus ve bu ölçümler model eğitimine eklenmedi.

İlk genişletilmiş denemede tek üretici bozuk bir ilgeç öbeğini kabul etmişti. “İlişkin/dair” benzeri bağlı sözcükleri ayıran bu hata son kodda engellendi. Aynı korpusta alınan daha eski 6/12 ve 7/12 sayıları bu son ortak korumalı karşılaştırmayla karıştırılmamalı. Otomatik kabul sayısı insan kalitesi veya anlam doğruluğu değildir.

| Sınama | Öneri oluşan metin | Uygulama bileşenleri tepe belleği |
|---|---:|---:|
| Windows — önceki beş alan | 3/5 | 1579 MiB |
| Windows — tek üretici / 12 metin | 5/12 | 1570 MiB |
| Windows — iki üretici / 12 metin | 8/12 | 1578 MiB |
| Linux — tek üretici / 12 metin | 5/12 | 1804 MiB |
| Linux — iki üretici / 12 metin | 8/12 | 1942 MiB |
| Linux — önceki beş alan | 4/5 | 1851 MiB |
| Linux — internet açık / önceki beş alan | 4/5 | 1932 MiB |

Bellek ölçümü Word ve işletim sistemini içermez. Öneri oluşması paragrafın tamamının yeniden yazıldığı anlamına gelmez.

Tanım, karmaşık karşıtlık ve bazı yan cümleler hâlâ sonuç vermeyebilir. Ek amaç/hedef çerçevesi serbest anlam anlayıcısı değildir. Türkçe dil bilgisi kontrolleri sınırlıdır. NLI tek başına olumsuzluk/özne değişimini kaçırır; ek kod denetimleri gereklidir. İnternet açık olduğunda sadece uygun kökler ve sabit yapı bankası adresi sorgulanır; paragraf sunucuya gönderilmez. İngilizce geniş sözlük taraması hâlâ bağlı değildir.

Sözlükte yerel tanım varsa web sorgusu atlanır. Kaynak hataları, HTTP/zaman aşımı türü ve geçici olarak atlanan sorgular ayrı gösterilir. Her kaynakta her kelimenin bulunacağı garanti edilmez.

## Windows — önceki beş alan

Olgu grafiği: 8 anlamı bozulmuş çiftte 0/8 yanlış kabul, 4 doğru çiftte 3/4 yanlış ret. Türkçe ek çerçeve + NLI: 0/6 yanlış kabul, 2/3 yanlış ret. Ek üretici İngilizceye uygulanmaz; İngilizce çiftleri bu ölçüme dahil değildir. NLI tek başına: 2/8 yanlış kabul, 1/4 yanlış ret. Bu küçük sınama genel doğruluk oranı değildir.

İptal çözümleyiciyi kapattı: True; anlam işlemini kapattı: True. Korpus SHA256: `a38b41287a8af00026c5e3b201f4eaf2f74019da7a0bcc129fa6a84e8a9c7187`.

### hukuk

Kaynak: Kurul, başvuruyu süresi içinde yapılması koşuluyla değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

Otomatik kabul edilen: Başvuruyu süresi içinde yapılması koşuluyla kurul değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

Sözlük kök taraması 16/16; internet kökü 4; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 15.2 saniye.

### muhendislik

Kaynak: Deney sırasında sensörün kaydettiği sıcaklık 42 °C olarak ölçülmüştür. Bu ölçüm, parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını göstermemektedir.

Denetimlerden geçen öneri yok.

Elenen: Deney sırasında sensörün kaydettiği sıcaklık 42 °C olarak ölçülmüştür. Parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını bu ölçüm göstermemektedir.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: kalacağını

Sözlük kök taraması 16/16; internet kökü 4; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 10.3 saniye.

### tarih

Kaynak: Arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Otomatik kabul edilen: Dönemin ticari ilişkileri hakkında arşivde bulunan mektuplar bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Elenen: Bilgi arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Sözlük kök taraması 18/18; internet kökü 6; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 15.5 saniye.

### egitim

Kaynak: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Araştırmacılar, bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini belirtmiştir.

Otomatik kabul edilen: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

Elenen: Dersin değerlendirme yönteminin açık olduğunu öğrencilerden alınan geri bildirimler göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: olduğunu; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Elenen: Dersin değerlendirme yönteminin açık olduğunu öğrencilerden alınan geri bildirimler göstermiştir. Araştırmacılar, bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini belirtmiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: olduğunu

Sözlük kök taraması 22/22; internet kökü 10; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 15.0 saniye.

### environment

Kaynak: The survey identified an association between tree cover and lower reported heat exposure. Because the data were observational, the authors did not claim that planting trees caused the difference.

Denetimlerden geçen öneri yok.

Elenen: The survey identified an association between tree cover and lower reported heat exposure. The authors did not claim that planting trees caused the difference because the data were observational.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: observational

Sözlük kök taraması 0/0; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 6.2 saniye.

## Windows — tek üretici / 12 metin

Olgu grafiği: 8 anlamı bozulmuş çiftte 0/8 yanlış kabul, 4 doğru çiftte 3/4 yanlış ret. Türkçe ek çerçeve + NLI: 0/6 yanlış kabul, 2/3 yanlış ret. Ek üretici İngilizceye uygulanmaz; İngilizce çiftleri bu ölçüme dahil değildir. NLI tek başına: 2/8 yanlış kabul, 1/4 yanlış ret. Bu küçük sınama genel doğruluk oranı değildir.

İptal çözümleyiciyi kapattı: True; anlam işlemini kapattı: True. Korpus SHA256: `f92547852c378c8e2c6fe6b2a61bf167df8141f185ccfe777c977e04803fd594`.

### recreation-purpose

Kaynak: Bu araştırmanın amacı, çalışanların serbest zaman tercihlerine ilişkin görüşlerini incelemektir.

Denetimlerden geçen öneri yok.

Elenen: Serbest zaman tercihlerine ilişkin bu araştırmanın amacı, çalışanların görüşlerini incelemektir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Görüşlerini bu araştırmanın amacı, çalışanların serbest zaman tercihlerine ilişkin incelemektir.

Gerekçeler: Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Sözlük kök taraması 10/10; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 8.5 saniye.

### recreation-definition

Kaynak: Rekreasyon, bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri kapsar. Katılımın zorunlu olması bu tanımla bağdaşmaz.

Otomatik kabul edilen: Rekreasyon, bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri kapsar. Bu tanımla katılımın zorunlu olması bağdaşmaz.

Elenen: Bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri rekreasyon kapsar. Bu tanımla katılımın zorunlu olması bağdaşmaz.

Gerekçeler: Korunan metin veya sırası değişmiş; Word eşlemesi yapılamadı.

Elenen: Bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri rekreasyon kapsar. Katılımın zorunlu olması bu tanımla bağdaşmaz.

Gerekçeler: Korunan metin veya sırası değişmiş; Word eşlemesi yapılamadı.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 3.6 saniye.

### psychology-evidence

Kaynak: Analiz sonuçları, sosyal desteğin yalnızlık düzeyini azalttığını göstermektedir. Bu ilişki, tek başına nedensellik kanıtı olarak değerlendirilemez.

Otomatik kabul edilen: Analiz sonuçları, sosyal desteğin yalnızlık düzeyini azalttığını göstermektedir. Tek başına bu ilişki nedensellik kanıtı olarak değerlendirilemez.

Sözlük kök taraması 16/16; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 3.6 saniye.

### agriculture-method

Kaynak: Bu çalışmada tohumların çimlenme oranları deneysel olarak karşılaştırılmıştır. Sulama miktarı bütün gruplarda aynı tutulmuştur.

Otomatik kabul edilen: Bu çalışmada tohumların çimlenme oranları deneysel olarak karşılaştırılmıştır. Bütün gruplarda sulama miktarı aynı tutulmuştur.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 3.6 saniye.

### economics-purpose

Kaynak: Bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla ilişkisini açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Denetimlerden geçen öneri yok.

Elenen: Gelirdeki değişimin harcama kararlarıyla bu incelemenin hedefi ilişkisini açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Elenen: İlişkisini bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Elenen: Gelirdeki değişimin harcama kararlarıyla bu incelemenin hedefi ilişkisini açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Elenen: İlişkisini bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Elenen: Bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla ilişkisini açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Sözlük kök taraması 15/15; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 8.7 saniye.

### engineering-procedure

Kaynak: Parçanın dayanıklılığı ölçülürken sıcaklık sabit tutulmuştur. Test sırasında uygulanan kuvvet 18 kN olarak kaydedilmiştir.

Denetimlerden geçen öneri yok.

Sözlük kök taraması 12/12; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 2.4 saniye.

### law-negative

Kaynak: Komisyon, itiraz dilekçesini süre koşulu bakımından incelememiştir. Dosyanın kayda alınması, başvurunun kabul edildiği anlamına gelmez.

Otomatik kabul edilen: İtiraz dilekçesini komisyon süre koşulu bakımından incelememiştir. Dosyanın kayda alınması, başvurunun kabul edildiği anlamına gelmez.

Sözlük kök taraması 15/15; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 4.9 saniye.

### history-contrast

Kaynak: Mektuplar incelenmiş olmakla birlikte, bazı tarihler kesin olarak belirlenememiştir. Bu belirsizlik, olayın gerçekleşmediğini kanıtlamaz.

Denetimlerden geçen öneri yok.

Elenen: Mektuplar incelenmiş olmakla birlikte, bazı tarihler kesin olarak belirlenememiştir. Olayın gerçekleşmediğini bu belirsizlik kanıtlamaz.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 3.6 saniye.

### education-purpose

Kaynak: Bu programın hedefi, öğrencilerin okuma becerilerini geliştirmektir. Programın bütün öğrencilerde aynı sonucu vereceği öne sürülmemiştir.

Denetimlerden geçen öneri yok.

Elenen: Öğrencilerin okuma becerilerini bu programın hedefi geliştirmektir. Öne programın bütün öğrencilerde aynı sonucu vereceği sürülmemiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Öğrencilerin okuma becerilerini bu programın hedefi geliştirmektir. Programın bütün öğrencilerde aynı sonucu vereceği öne sürülmemiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Elenen: Bu programın hedefi, öğrencilerin okuma becerilerini geliştirmektir. Öne programın bütün öğrencilerde aynı sonucu vereceği sürülmemiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Sözlük kök taraması 13/13; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 6.2 saniye.

### health-observational

Kaynak: Anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında ilişki bulunmuştur. Bu bulgu, kısa uykunun yorgunluğa neden olduğunu kanıtlamaz.

Denetimlerden geçen öneri yok.

Elenen: İlişki anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında bulunmuştur. Kısa uykunun yorgunluğa neden olduğunu bu bulgu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: İlişki anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında bulunmuştur. Bu bulgu, kısa uykunun yorgunluğa neden olduğunu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında ilişki bulunmuştur. Kısa uykunun yorgunluğa neden olduğunu bu bulgu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Sözlük kök taraması 16/16; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 6.3 saniye.

### english-audit

Kaynak: Auditors reviewed the expenditure records. They did not conclude that every payment was improper.

Otomatik kabul edilen: The expenditure records were reviewed by auditors. They did not conclude that every payment was improper.

Sözlük kök taraması 0/0; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 6.1 saniye.

### english-recreation

Kaynak: Participation in community activities was associated with higher reported life satisfaction. The observational design did not establish causation.

Denetimlerden geçen öneri yok.

Sözlük kök taraması 0/0; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 1.7 saniye.

## Windows — iki üretici / 12 metin

Olgu grafiği: 8 anlamı bozulmuş çiftte 0/8 yanlış kabul, 4 doğru çiftte 3/4 yanlış ret. Türkçe ek çerçeve + NLI: 0/6 yanlış kabul, 2/3 yanlış ret. Ek üretici İngilizceye uygulanmaz; İngilizce çiftleri bu ölçüme dahil değildir. NLI tek başına: 2/8 yanlış kabul, 1/4 yanlış ret. Bu küçük sınama genel doğruluk oranı değildir.

İptal çözümleyiciyi kapattı: True; anlam işlemini kapattı: True. Korpus SHA256: `f92547852c378c8e2c6fe6b2a61bf167df8141f185ccfe777c977e04803fd594`.

### recreation-purpose

Kaynak: Bu araştırmanın amacı, çalışanların serbest zaman tercihlerine ilişkin görüşlerini incelemektir.

Otomatik kabul edilen: Bu araştırma, çalışanların serbest zaman tercihlerine ilişkin görüşlerini incelemeyi amaçlamaktadır.

Elenen: Serbest zaman tercihlerine ilişkin bu araştırmanın amacı, çalışanların görüşlerini incelemektir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Görüşlerini bu araştırmanın amacı, çalışanların serbest zaman tercihlerine ilişkin incelemektir.

Gerekçeler: Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Sözlük kök taraması 10/10; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 10.0 saniye.

### recreation-definition

Kaynak: Rekreasyon, bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri kapsar. Katılımın zorunlu olması bu tanımla bağdaşmaz.

Otomatik kabul edilen: Rekreasyon, bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri kapsar. Bu tanımla katılımın zorunlu olması bağdaşmaz.

Elenen: Bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri rekreasyon kapsar. Bu tanımla katılımın zorunlu olması bağdaşmaz.

Gerekçeler: Korunan metin veya sırası değişmiş; Word eşlemesi yapılamadı.

Elenen: Bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri rekreasyon kapsar. Katılımın zorunlu olması bu tanımla bağdaşmaz.

Gerekçeler: Korunan metin veya sırası değişmiş; Word eşlemesi yapılamadı.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 3.6 saniye.

### psychology-evidence

Kaynak: Analiz sonuçları, sosyal desteğin yalnızlık düzeyini azalttığını göstermektedir. Bu ilişki, tek başına nedensellik kanıtı olarak değerlendirilemez.

Otomatik kabul edilen: Analiz sonuçları, sosyal desteğin yalnızlık düzeyini azalttığını göstermektedir. Tek başına bu ilişki nedensellik kanıtı olarak değerlendirilemez.

Elenen: Sosyal desteğin yalnızlık düzeyini azalttığı, analiz sonuçlarıyla gösterilmektedir. Bu ilişki, tek başına nedensellik kanıtı olarak değerlendirilemez.

Gerekçeler: Yeniden çözümlemede ana yüklem değişti.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Sözlük kök taraması 16/16; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 4.9 saniye.

### agriculture-method

Kaynak: Bu çalışmada tohumların çimlenme oranları deneysel olarak karşılaştırılmıştır. Sulama miktarı bütün gruplarda aynı tutulmuştur.

Otomatik kabul edilen: Bu çalışmada tohumların çimlenme oranları deneysel olarak karşılaştırılmıştır. Bütün gruplarda sulama miktarı aynı tutulmuştur.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 3.6 saniye.

### economics-purpose

Kaynak: Bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla ilişkisini açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Otomatik kabul edilen: Bu inceleme, gelirdeki değişimin harcama kararlarıyla ilişkisini açıklamayı hedeflemektedir. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Elenen: Gelirdeki değişimin harcama kararlarıyla bu incelemenin hedefi ilişkisini açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Elenen: İlişkisini bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Elenen: Gelirdeki değişimin harcama kararlarıyla bu incelemenin hedefi ilişkisini açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Elenen: İlişkisini bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Elenen: Bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla ilişkisini açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Sözlük kök taraması 15/15; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 10.1 saniye.

### engineering-procedure

Kaynak: Parçanın dayanıklılığı ölçülürken sıcaklık sabit tutulmuştur. Test sırasında uygulanan kuvvet 18 kN olarak kaydedilmiştir.

Denetimlerden geçen öneri yok.

Sözlük kök taraması 12/12; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 2.4 saniye.

### law-negative

Kaynak: Komisyon, itiraz dilekçesini süre koşulu bakımından incelememiştir. Dosyanın kayda alınması, başvurunun kabul edildiği anlamına gelmez.

Otomatik kabul edilen: İtiraz dilekçesini komisyon süre koşulu bakımından incelememiştir. Dosyanın kayda alınması, başvurunun kabul edildiği anlamına gelmez.

Sözlük kök taraması 15/15; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 4.9 saniye.

### history-contrast

Kaynak: Mektuplar incelenmiş olmakla birlikte, bazı tarihler kesin olarak belirlenememiştir. Bu belirsizlik, olayın gerçekleşmediğini kanıtlamaz.

Denetimlerden geçen öneri yok.

Elenen: Mektuplar incelenmiş olmakla birlikte, bazı tarihler kesin olarak belirlenememiştir. Olayın gerçekleşmediğini bu belirsizlik kanıtlamaz.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Elenen: Mektuplar incelenmiştir; bununla birlikte bazı tarihler kesin olarak belirlenememiştir. Bu belirsizlik, olayın gerçekleşmediğini kanıtlamaz.

Gerekçeler: Yeniden çözümlemede ana yüklem değişti.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 4.9 saniye.

### education-purpose

Kaynak: Bu programın hedefi, öğrencilerin okuma becerilerini geliştirmektir. Programın bütün öğrencilerde aynı sonucu vereceği öne sürülmemiştir.

Otomatik kabul edilen: Bu program, öğrencilerin okuma becerilerini geliştirmeyi hedeflemektedir. Programın bütün öğrencilerde aynı sonucu vereceği öne sürülmemiştir.

Elenen: Öğrencilerin okuma becerilerini bu programın hedefi geliştirmektir. Öne programın bütün öğrencilerde aynı sonucu vereceği sürülmemiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Öğrencilerin okuma becerilerini bu programın hedefi geliştirmektir. Programın bütün öğrencilerde aynı sonucu vereceği öne sürülmemiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Elenen: Bu programın hedefi, öğrencilerin okuma becerilerini geliştirmektir. Öne programın bütün öğrencilerde aynı sonucu vereceği sürülmemiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Sözlük kök taraması 13/13; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 7.4 saniye.

### health-observational

Kaynak: Anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında ilişki bulunmuştur. Bu bulgu, kısa uykunun yorgunluğa neden olduğunu kanıtlamaz.

Denetimlerden geçen öneri yok.

Elenen: İlişki anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında bulunmuştur. Kısa uykunun yorgunluğa neden olduğunu bu bulgu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: İlişki anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında bulunmuştur. Bu bulgu, kısa uykunun yorgunluğa neden olduğunu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında ilişki bulunmuştur. Kısa uykunun yorgunluğa neden olduğunu bu bulgu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Sözlük kök taraması 16/16; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 6.3 saniye.

### english-audit

Kaynak: Auditors reviewed the expenditure records. They did not conclude that every payment was improper.

Otomatik kabul edilen: The expenditure records were reviewed by auditors. They did not conclude that every payment was improper.

Sözlük kök taraması 0/0; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 6.2 saniye.

### english-recreation

Kaynak: Participation in community activities was associated with higher reported life satisfaction. The observational design did not establish causation.

Denetimlerden geçen öneri yok.

Sözlük kök taraması 0/0; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 1.7 saniye.

## Linux — tek üretici / 12 metin

Olgu grafiği: 8 anlamı bozulmuş çiftte 0/8 yanlış kabul, 4 doğru çiftte 2/4 yanlış ret. Türkçe ek çerçeve + NLI: 0/6 yanlış kabul, 2/3 yanlış ret. Ek üretici İngilizceye uygulanmaz; İngilizce çiftleri bu ölçüme dahil değildir. NLI tek başına: 2/8 yanlış kabul, 1/4 yanlış ret. Bu küçük sınama genel doğruluk oranı değildir.

İptal çözümleyiciyi kapattı: True; anlam işlemini kapattı: True. Korpus SHA256: `f92547852c378c8e2c6fe6b2a61bf167df8141f185ccfe777c977e04803fd594`.

### recreation-purpose

Kaynak: Bu araştırmanın amacı, çalışanların serbest zaman tercihlerine ilişkin görüşlerini incelemektir.

Denetimlerden geçen öneri yok.

Elenen: Çalışanların serbest zaman tercihlerine bu araştırmanın amacı ilişkin görüşlerini incelemektir.

Gerekçeler: Hâl ekiyle bağlı ilgeç öbeği bölündü veya ilişkisi değişti.; Hâl ekiyle bağlı ilgeç öbeği bölündü veya ilişkisi değişti.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Sözlük kök taraması 10/10; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 8.7 saniye.

### recreation-definition

Kaynak: Rekreasyon, bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri kapsar. Katılımın zorunlu olması bu tanımla bağdaşmaz.

Denetimlerden geçen öneri yok.

Elenen: Bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri rekreasyon kapsar. Katılımın zorunlu olması bu tanımla bağdaşmaz.

Gerekçeler: Korunan metin veya sırası değişmiş; Word eşlemesi yapılamadı.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 2.6 saniye.

### psychology-evidence

Kaynak: Analiz sonuçları, sosyal desteğin yalnızlık düzeyini azalttığını göstermektedir. Bu ilişki, tek başına nedensellik kanıtı olarak değerlendirilemez.

Otomatik kabul edilen: Analiz sonuçları, sosyal desteğin yalnızlık düzeyini azalttığını göstermektedir. Tek başına bu ilişki nedensellik kanıtı olarak değerlendirilemez.

Elenen: Sosyal desteğin yalnızlık düzeyini azalttığını analiz sonuçları göstermektedir. Tek başına bu ilişki nedensellik kanıtı olarak değerlendirilemez.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: azalttığını; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Elenen: Sosyal desteğin yalnızlık düzeyini azalttığını analiz sonuçları göstermektedir. Bu ilişki, tek başına nedensellik kanıtı olarak değerlendirilemez.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: azalttığını

Sözlük kök taraması 16/16; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 6.7 saniye.

### agriculture-method

Kaynak: Bu çalışmada tohumların çimlenme oranları deneysel olarak karşılaştırılmıştır. Sulama miktarı bütün gruplarda aynı tutulmuştur.

Otomatik kabul edilen: Bu çalışmada tohumların çimlenme oranları deneysel olarak karşılaştırılmıştır. Bütün gruplarda sulama miktarı aynı tutulmuştur.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 3.9 saniye.

### economics-purpose

Kaynak: Bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla ilişkisini açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Denetimlerden geçen öneri yok.

Elenen: Gelirdeki değişimin harcama kararlarıyla bu incelemenin hedefi ilişkisini açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Elenen: İlişkisini bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Elenen: Gelirdeki değişimin harcama kararlarıyla bu incelemenin hedefi ilişkisini açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Elenen: İlişkisini bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Elenen: Bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla ilişkisini açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Sözlük kök taraması 15/15; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 9.5 saniye.

### engineering-procedure

Kaynak: Parçanın dayanıklılığı ölçülürken sıcaklık sabit tutulmuştur. Test sırasında uygulanan kuvvet 18 kN olarak kaydedilmiştir.

Denetimlerden geçen öneri yok.

Elenen: Sıcaklık parçanın dayanıklılığı ölçülürken sabit tutulmuştur. Test sırasında uygulanan kuvvet 18 kN olarak kaydedilmiştir.

Gerekçeler: Aynı yüklemin belirtili nesnesi yeni cümlede kayboldu/değişti: dayanıklı; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Sözlük kök taraması 12/12; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 3.9 saniye.

### law-negative

Kaynak: Komisyon, itiraz dilekçesini süre koşulu bakımından incelememiştir. Dosyanın kayda alınması, başvurunun kabul edildiği anlamına gelmez.

Otomatik kabul edilen: İtiraz dilekçesini komisyon süre koşulu bakımından incelememiştir. Dosyanın kayda alınması, başvurunun kabul edildiği anlamına gelmez.

Elenen: İtiraz dilekçesini komisyon süre koşulu bakımından incelememiştir. Başvurunun kabul edildiği anlamına dosyanın kayda alınması gelmez.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Süre koşulu bakımından komisyon, itiraz dilekçesini incelememiştir. Başvurunun kabul edildiği anlamına dosyanın kayda alınması gelmez.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Komisyon, itiraz dilekçesini süre koşulu bakımından incelememiştir. Başvurunun kabul edildiği anlamına dosyanın kayda alınması gelmez.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Sözlük kök taraması 15/15; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 9.5 saniye.

### history-contrast

Kaynak: Mektuplar incelenmiş olmakla birlikte, bazı tarihler kesin olarak belirlenememiştir. Bu belirsizlik, olayın gerçekleşmediğini kanıtlamaz.

Denetimlerden geçen öneri yok.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 2.6 saniye.

### education-purpose

Kaynak: Bu programın hedefi, öğrencilerin okuma becerilerini geliştirmektir. Programın bütün öğrencilerde aynı sonucu vereceği öne sürülmemiştir.

Denetimlerden geçen öneri yok.

Elenen: Öğrencilerin okuma becerilerini bu programın hedefi geliştirmektir. Programın bütün öğrencilerde aynı sonucu vereceği öne sürülmemiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Sözlük kök taraması 13/13; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 4.0 saniye.

### health-observational

Kaynak: Anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında ilişki bulunmuştur. Bu bulgu, kısa uykunun yorgunluğa neden olduğunu kanıtlamaz.

Otomatik kabul edilen: Uyku süresi ile bildirilen yorgunluk arasında anket verilerinde ilişki bulunmuştur. Bu bulgu, kısa uykunun yorgunluğa neden olduğunu kanıtlamaz.

Elenen: Uyku süresi ile bildirilen yorgunluk arasında anket verilerinde ilişki bulunmuştur. Kısa uykunun yorgunluğa neden olduğunu bu bulgu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Elenen: İlişki anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında bulunmuştur. Kısa uykunun yorgunluğa neden olduğunu bu bulgu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: İlişki anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında bulunmuştur. Bu bulgu, kısa uykunun yorgunluğa neden olduğunu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında ilişki bulunmuştur. Kısa uykunun yorgunluğa neden olduğunu bu bulgu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Sözlük kök taraması 16/16; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 9.9 saniye.

### english-audit

Kaynak: Auditors reviewed the expenditure records. They did not conclude that every payment was improper.

Otomatik kabul edilen: The expenditure records were reviewed by auditors. They did not conclude that every payment was improper.

Sözlük kök taraması 0/0; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 7.3 saniye.

### english-recreation

Kaynak: Participation in community activities was associated with higher reported life satisfaction. The observational design did not establish causation.

Denetimlerden geçen öneri yok.

Sözlük kök taraması 0/0; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 1.9 saniye.

## Linux — iki üretici / 12 metin

Olgu grafiği: 8 anlamı bozulmuş çiftte 0/8 yanlış kabul, 4 doğru çiftte 2/4 yanlış ret. Türkçe ek çerçeve + NLI: 0/6 yanlış kabul, 2/3 yanlış ret. Ek üretici İngilizceye uygulanmaz; İngilizce çiftleri bu ölçüme dahil değildir. NLI tek başına: 2/8 yanlış kabul, 1/4 yanlış ret. Bu küçük sınama genel doğruluk oranı değildir.

İptal çözümleyiciyi kapattı: True; anlam işlemini kapattı: True. Korpus SHA256: `f92547852c378c8e2c6fe6b2a61bf167df8141f185ccfe777c977e04803fd594`.

### recreation-purpose

Kaynak: Bu araştırmanın amacı, çalışanların serbest zaman tercihlerine ilişkin görüşlerini incelemektir.

Otomatik kabul edilen: Bu araştırma, çalışanların serbest zaman tercihlerine ilişkin görüşlerini incelemeyi amaçlamaktadır.

Elenen: Çalışanların serbest zaman tercihlerine bu araştırmanın amacı ilişkin görüşlerini incelemektir.

Gerekçeler: Hâl ekiyle bağlı ilgeç öbeği bölündü veya ilişkisi değişti.; Hâl ekiyle bağlı ilgeç öbeği bölündü veya ilişkisi değişti.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Sözlük kök taraması 10/10; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 9.3 saniye.

### recreation-definition

Kaynak: Rekreasyon, bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri kapsar. Katılımın zorunlu olması bu tanımla bağdaşmaz.

Denetimlerden geçen öneri yok.

Elenen: Bireylerin serbest zamanlarında gönüllü olarak katıldıkları etkinlikleri rekreasyon kapsar. Katılımın zorunlu olması bu tanımla bağdaşmaz.

Gerekçeler: Korunan metin veya sırası değişmiş; Word eşlemesi yapılamadı.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 2.6 saniye.

### psychology-evidence

Kaynak: Analiz sonuçları, sosyal desteğin yalnızlık düzeyini azalttığını göstermektedir. Bu ilişki, tek başına nedensellik kanıtı olarak değerlendirilemez.

Otomatik kabul edilen: Analiz sonuçları, sosyal desteğin yalnızlık düzeyini azalttığını göstermektedir. Tek başına bu ilişki nedensellik kanıtı olarak değerlendirilemez.

Elenen: Sosyal desteğin yalnızlık düzeyini azalttığını analiz sonuçları göstermektedir. Tek başına bu ilişki nedensellik kanıtı olarak değerlendirilemez.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: azalttığını; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Elenen: Sosyal desteğin yalnızlık düzeyini azalttığını analiz sonuçları göstermektedir. Bu ilişki, tek başına nedensellik kanıtı olarak değerlendirilemez.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: azalttığını

Elenen: Sosyal desteğin yalnızlık düzeyini azalttığı, analiz sonuçlarıyla gösterilmektedir. Bu ilişki, tek başına nedensellik kanıtı olarak değerlendirilemez.

Gerekçeler: Yeniden çözümlemede ana yüklem değişti.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Sözlük kök taraması 16/16; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 8.2 saniye.

### agriculture-method

Kaynak: Bu çalışmada tohumların çimlenme oranları deneysel olarak karşılaştırılmıştır. Sulama miktarı bütün gruplarda aynı tutulmuştur.

Otomatik kabul edilen: Bu çalışmada tohumların çimlenme oranları deneysel olarak karşılaştırılmıştır. Bütün gruplarda sulama miktarı aynı tutulmuştur.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 3.9 saniye.

### economics-purpose

Kaynak: Bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla ilişkisini açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Otomatik kabul edilen: Bu inceleme, gelirdeki değişimin harcama kararlarıyla ilişkisini açıklamayı hedeflemektedir. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Elenen: Gelirdeki değişimin harcama kararlarıyla bu incelemenin hedefi ilişkisini açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Elenen: İlişkisini bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Elenen: Gelirdeki değişimin harcama kararlarıyla bu incelemenin hedefi ilişkisini açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Elenen: İlişkisini bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla açıklamaktır. Kullanılan veriler iki yılın gözlemleriyle sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Elenen: Bu incelemenin hedefi, gelirdeki değişimin harcama kararlarıyla ilişkisini açıklamaktır. İki yılın gözlemleriyle kullanılan veriler sınırlıdır.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Sözlük kök taraması 15/15; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 11.2 saniye.

### engineering-procedure

Kaynak: Parçanın dayanıklılığı ölçülürken sıcaklık sabit tutulmuştur. Test sırasında uygulanan kuvvet 18 kN olarak kaydedilmiştir.

Denetimlerden geçen öneri yok.

Elenen: Sıcaklık parçanın dayanıklılığı ölçülürken sabit tutulmuştur. Test sırasında uygulanan kuvvet 18 kN olarak kaydedilmiştir.

Gerekçeler: Aynı yüklemin belirtili nesnesi yeni cümlede kayboldu/değişti: dayanıklı; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Sözlük kök taraması 12/12; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 4.0 saniye.

### law-negative

Kaynak: Komisyon, itiraz dilekçesini süre koşulu bakımından incelememiştir. Dosyanın kayda alınması, başvurunun kabul edildiği anlamına gelmez.

Otomatik kabul edilen: İtiraz dilekçesini komisyon süre koşulu bakımından incelememiştir. Dosyanın kayda alınması, başvurunun kabul edildiği anlamına gelmez.

Elenen: İtiraz dilekçesini komisyon süre koşulu bakımından incelememiştir. Başvurunun kabul edildiği anlamına dosyanın kayda alınması gelmez.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Süre koşulu bakımından komisyon, itiraz dilekçesini incelememiştir. Başvurunun kabul edildiği anlamına dosyanın kayda alınması gelmez.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Komisyon, itiraz dilekçesini süre koşulu bakımından incelememiştir. Başvurunun kabul edildiği anlamına dosyanın kayda alınması gelmez.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Sözlük kök taraması 15/15; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 10.5 saniye.

### history-contrast

Kaynak: Mektuplar incelenmiş olmakla birlikte, bazı tarihler kesin olarak belirlenememiştir. Bu belirsizlik, olayın gerçekleşmediğini kanıtlamaz.

Denetimlerden geçen öneri yok.

Elenen: Mektuplar incelenmiştir; bununla birlikte bazı tarihler kesin olarak belirlenememiştir. Bu belirsizlik, olayın gerçekleşmediğini kanıtlamaz.

Gerekçeler: Yeniden çözümlemede ana yüklem değişti.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.

Sözlük kök taraması 14/14; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 4.3 saniye.

### education-purpose

Kaynak: Bu programın hedefi, öğrencilerin okuma becerilerini geliştirmektir. Programın bütün öğrencilerde aynı sonucu vereceği öne sürülmemiştir.

Otomatik kabul edilen: Bu program, öğrencilerin okuma becerilerini geliştirmeyi hedeflemektedir. Programın bütün öğrencilerde aynı sonucu vereceği öne sürülmemiştir.

Elenen: Öğrencilerin okuma becerilerini bu programın hedefi geliştirmektir. Programın bütün öğrencilerde aynı sonucu vereceği öne sürülmemiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.

Sözlük kök taraması 13/13; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 5.8 saniye.

### health-observational

Kaynak: Anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında ilişki bulunmuştur. Bu bulgu, kısa uykunun yorgunluğa neden olduğunu kanıtlamaz.

Otomatik kabul edilen: Uyku süresi ile bildirilen yorgunluk arasında anket verilerinde ilişki bulunmuştur. Bu bulgu, kısa uykunun yorgunluğa neden olduğunu kanıtlamaz.

Elenen: Uyku süresi ile bildirilen yorgunluk arasında anket verilerinde ilişki bulunmuştur. Kısa uykunun yorgunluğa neden olduğunu bu bulgu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Elenen: İlişki anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında bulunmuştur. Kısa uykunun yorgunluğa neden olduğunu bu bulgu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: İlişki anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında bulunmuştur. Bu bulgu, kısa uykunun yorgunluğa neden olduğunu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Anket verilerinde, uyku süresi ile bildirilen yorgunluk arasında ilişki bulunmuştur. Kısa uykunun yorgunluğa neden olduğunu bu bulgu kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Sözlük kök taraması 16/16; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 9.9 saniye.

### english-audit

Kaynak: Auditors reviewed the expenditure records. They did not conclude that every payment was improper.

Otomatik kabul edilen: The expenditure records were reviewed by auditors. They did not conclude that every payment was improper.

Sözlük kök taraması 0/0; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 7.1 saniye.

### english-recreation

Kaynak: Participation in community activities was associated with higher reported life satisfaction. The observational design did not establish causation.

Denetimlerden geçen öneri yok.

Sözlük kök taraması 0/0; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 1.9 saniye.

## Linux — önceki beş alan

Olgu grafiği: 8 anlamı bozulmuş çiftte 0/8 yanlış kabul, 4 doğru çiftte 2/4 yanlış ret. Türkçe ek çerçeve + NLI: 0/6 yanlış kabul, 2/3 yanlış ret. Ek üretici İngilizceye uygulanmaz; İngilizce çiftleri bu ölçüme dahil değildir. NLI tek başına: 2/8 yanlış kabul, 1/4 yanlış ret. Bu küçük sınama genel doğruluk oranı değildir.

İptal çözümleyiciyi kapattı: True; anlam işlemini kapattı: True. Korpus SHA256: `a38b41287a8af00026c5e3b201f4eaf2f74019da7a0bcc129fa6a84e8a9c7187`.

### hukuk

Kaynak: Kurul, başvuruyu süresi içinde yapılması koşuluyla değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

Otomatik kabul edilen: Başvuruyu süresi içinde yapılması koşuluyla kurul değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

Sözlük kök taraması 16/16; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 7.5 saniye.

### muhendislik

Kaynak: Deney sırasında sensörün kaydettiği sıcaklık 42 °C olarak ölçülmüştür. Bu ölçüm, parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını göstermemektedir.

Denetimlerden geçen öneri yok.

Elenen: Deney sırasında sensörün kaydettiği sıcaklık 42 °C olarak ölçülmüştür. Parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını bu ölçüm göstermemektedir.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: kalacağını

Sözlük kök taraması 16/16; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 4.0 saniye.

### tarih

Kaynak: Arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Otomatik kabul edilen: Dönemin ticari ilişkileri hakkında arşivde bulunan mektuplar bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Elenen: Dönemin ticari ilişkileri hakkında arşivde bulunan mektuplar bilgi sağlamaktadır. Ancak o olayın hiç yaşanmadığını belgelerde bir olayın yer almaması kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede ana yüklem değişti.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: yaşanmadığını; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Bilgi arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında sağlamaktadır. Ancak o olayın hiç yaşanmadığını belgelerde bir olayın yer almaması kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede ana yüklem değişti.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: yaşanmadığını; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Bilgi arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Elenen: Arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında bilgi sağlamaktadır. Ancak o olayın hiç yaşanmadığını belgelerde bir olayın yer almaması kanıtlamaz.

Gerekçeler: Yeniden çözümlemede ana yüklem değişti.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: yaşanmadığını; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Sözlük kök taraması 18/18; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 9.9 saniye.

### egitim

Kaynak: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Araştırmacılar, bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini belirtmiştir.

Otomatik kabul edilen: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

Elenen: Dersin değerlendirme yönteminin açık olduğunu öğrencilerden alınan geri bildirimler göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: olduğunu; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: gerektiğini

Elenen: Dersin değerlendirme yönteminin açık olduğunu öğrencilerden alınan geri bildirimler göstermiştir. Araştırmacılar, bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini belirtmiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: olduğunu

Sözlük kök taraması 22/22; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 6.8 saniye.

### environment

Kaynak: The survey identified an association between tree cover and lower reported heat exposure. Because the data were observational, the authors did not claim that planting trees caused the difference.

Otomatik kabul edilen: An association between tree cover and lower reported heat exposure was identified by the survey. Because the data were observational, the authors did not claim that planting trees caused the difference.

Elenen: An association between tree cover and lower reported heat exposure was identified by the survey. The authors did not claim that planting trees caused the difference because the data were observational.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: observational; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: caused

Elenen: The survey identified an association between tree cover and lower reported heat exposure. The authors did not claim that planting trees caused the difference because the data were observational.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: observational; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: caused

Sözlük kök taraması 0/0; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 9.2 saniye.

## Linux — internet açık / önceki beş alan

Olgu grafiği: 8 anlamı bozulmuş çiftte 0/8 yanlış kabul, 4 doğru çiftte 2/4 yanlış ret. Türkçe ek çerçeve + NLI: 0/6 yanlış kabul, 2/3 yanlış ret. Ek üretici İngilizceye uygulanmaz; İngilizce çiftleri bu ölçüme dahil değildir. NLI tek başına: 2/8 yanlış kabul, 1/4 yanlış ret. Bu küçük sınama genel doğruluk oranı değildir.

İptal çözümleyiciyi kapattı: True; anlam işlemini kapattı: True. Korpus SHA256: `a38b41287a8af00026c5e3b201f4eaf2f74019da7a0bcc129fa6a84e8a9c7187`.

### hukuk

Kaynak: Kurul, başvuruyu süresi içinde yapılması koşuluyla değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

Otomatik kabul edilen: Başvuruyu süresi içinde yapılması koşuluyla kurul değerlendirecektir. Başvuru dosyasındaki belgelerin eksiksiz olması, talebin kabul edildiği anlamına gelmez.

Sözlük kök taraması 16/16; internet kökü 4; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 21.4 saniye.

### muhendislik

Kaynak: Deney sırasında sensörün kaydettiği sıcaklık 42 °C olarak ölçülmüştür. Bu ölçüm, parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını göstermemektedir.

Denetimlerden geçen öneri yok.

Elenen: Deney sırasında sensörün kaydettiği sıcaklık 42 °C olarak ölçülmüştür. Parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını bu ölçüm göstermemektedir.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: kalacağını

Sözlük kök taraması 16/16; internet kökü 4; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 11.8 saniye.

### tarih

Kaynak: Arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Otomatik kabul edilen: Dönemin ticari ilişkileri hakkında arşivde bulunan mektuplar bilgi sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Elenen: Dönemin ticari ilişkileri hakkında arşivde bulunan mektuplar bilgi sağlamaktadır. Ancak o olayın hiç yaşanmadığını belgelerde bir olayın yer almaması kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede ana yüklem değişti.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: yaşanmadığını; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Bilgi arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında sağlamaktadır. Ancak o olayın hiç yaşanmadığını belgelerde bir olayın yer almaması kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yeniden çözümlemede ana yüklem değişti.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: yaşanmadığını; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Elenen: Bilgi arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında sağlamaktadır. Ancak belgelerde bir olayın yer almaması, o olayın hiç yaşanmadığını kanıtlamaz.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.

Elenen: Arşivde bulunan mektuplar, dönemin ticari ilişkileri hakkında bilgi sağlamaktadır. Ancak o olayın hiç yaşanmadığını belgelerde bir olayın yer almaması kanıtlamaz.

Gerekçeler: Yeniden çözümlemede ana yüklem değişti.; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: yaşanmadığını; Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.; Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.

Sözlük kök taraması 18/18; internet kökü 6; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 23.0 saniye.

### egitim

Kaynak: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Araştırmacılar, bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini belirtmiştir.

Otomatik kabul edilen: Öğrencilerden alınan geri bildirimler, dersin değerlendirme yönteminin açık olduğunu göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

Elenen: Dersin değerlendirme yönteminin açık olduğunu öğrencilerden alınan geri bildirimler göstermiştir. Bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini araştırmacılar belirtmiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: olduğunu; Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: gerektiğini

Elenen: Dersin değerlendirme yönteminin açık olduğunu öğrencilerden alınan geri bildirimler göstermiştir. Araştırmacılar, bu bulgunun öğrencilerin başarı düzeyine ilişkin bir sonuç olarak yorumlanmaması gerektiğini belirtmiştir.

Gerekçeler: Yeniden çözümlemede eyleyen ilişkisi değişti veya kayboldu.; Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: olduğunu

Sözlük kök taraması 22/22; internet kökü 10; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 18.4 saniye.

### environment

Kaynak: The survey identified an association between tree cover and lower reported heat exposure. Because the data were observational, the authors did not claim that planting trees caused the difference.

Otomatik kabul edilen: An association between tree cover and lower reported heat exposure was identified by the survey. Because the data were observational, the authors did not claim that planting trees caused the difference.

Elenen: An association between tree cover and lower reported heat exposure was identified by the survey. The authors did not claim that planting trees caused the difference because the data were observational.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: observational; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: caused

Elenen: The survey identified an association between tree cover and lower reported heat exposure. The authors did not claim that planting trees caused the difference because the data were observational.

Gerekçeler: Yeniden çözümlemede nesne/önerme ilişkisi değişti veya kayboldu.; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: observational; Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: caused

Sözlük kök taraması 0/0; internet kökü 0; gerçek sağlayıcı hatası 0; atlanan 0; hata türleri {}.
Süre: 9.8 saniye.

## Kurulum özeti

fed77506fd13d501f0c60ce27426431a6831f3207ac23e331c91811f412a28ee  AkademikParafraz-1.7.0-Setup.exe
