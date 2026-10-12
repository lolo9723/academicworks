# Genel backend denetimi ve güçlendirme sırası

12 Ekim 2026. Bu çalışma kullanıcının tek bir örneğini uyarlamak yerine varsayılan motorun genel çağrı zincirini inceler. Kullanıcıya ait rekreasyon paragrafı alınmadı; canlı sınamalarda sentetik/genel metinler kullanıldı.

## Doğrulanan sorunlar

- 1.6.0 Word varsayılanı olan MultiStageRewriteEngine, WordNetLexicalSource/DictionaryService almıyordu. İnternetle Parafraz komutu ayarı açsa da bu motor sözlük sorgusu yapmıyordu. Diğer motorlardaki sözlük testinin geçmesi bu bağlantıyı sınamıyordu.
- Word denetleyicisi yeni motorun MultiStageDiagnostics nesnesini almıyordu; üretim yetersizliği, korunan Word alanları ve adayların reddi aynı genel mesajla sonuçlanıyordu.
- 2.400 karakterden uzun paragraflar sessizce atlanıyordu. Artık mevcut cümle sınırlarıyla parçalanıyor; tek başına fazla uzun cümle atlanırsa açıklanıyor. Bu bir paragraf anlam planlayıcısı değildir.
- Eski kural motorunun düzenleme içermeyen kaynak metin adayı Word panelinde öneri olarak gösterilebiliyordu. Artık gerçek değişiklik içermeyen adaylar gösterilmiyor.
- Yeni Türkçe üretim yalnızca nesne/tümleç/önerme öbeğini başa taşır. İsim cümlesi, tanım, farklı fiille anlatım ve cümle bölme/birleştirme için genel üretim yoktur.
- Anlam denetimi ana fiil kökünü, bütün fiil kökleri listesini ve cümle sayısını aynı tutmayı bekler. Bu koşullar serbest parafraz için uygun değildir. 1.6.0 Windows sınamasında dört doğru anlam çiftinin üçü olgu denetimince reddedilmiştir; bu küçük örnek genel bir hata oranı değildir.
- Türkçe dil bilgisi denetimi tam bir düzeltici değildir. NLI sınıflandırıcısı da tek başına olumsuzluk ve özne/nesne değişimlerini kaçırır.

## 1.6.1 düzeltmesinin kapsamı

Türkçe varsayılan motor yerel sözlüğe bağlanır; internet açık ve çevrimdışı kapalıysa mevcut sağlayıcılar uygun kökler için çağrılır. Özel isimler, korunan terimler, bağlantılar ve düzenlenemeyen Word alanları tarama dışında tutulur. Tam metin sağlayıcılara gönderilmez. Sözlük bilgisi cümle yapısı üretiminin yerini almaz; otomatik kelime değişimi eklenmedi.

Tarama sayaçları paragraflar/parçalar boyunca toplanır. Çıktı olmasa bile tarama ve ret gerekçesi Word paneline aktarılır. İngilizce geniş sözlük taramasının ve internet yapı bankasının bu motorda kullanılmadığı açıkça belirtilir. Yeni kuruluş aileleri veya genel üretici model bu düzeltmeye eklenmedi; 1.6.0 dil kalitesi sınırları devam eder.

Canlı bağımsız sözlük sınamasında beş genel terimin tamamı en az bir kaynakta bulundu; toplam yedi bilgi yanıtı geldi, bağlantı hatası sıfırdı. Bu geliştirici ortamı sınamasıdır; kullanıcının bilgisayarındaki ağ bağlantısını doğrulamaz. Varsayılan motorun sözlüğü gerçekten çağırdığı ayrıca yeni uçtan uca BackendProbe ile sınanır.

## Güçlendirme için önerilen sıra

1. **Üretim ve denetim sözleşmesini birlikte değiştir.** Cümleyi “kim, ne yaptı, neye, hangi koşulla, hangi kesinlikte” önermelerine ayır; adayları bu önermelerle karşılaştır. Aynı sözcüklerin ve aynı fiil köklerinin bulunmasını genel kabul koşulu olmaktan çıkar. Olumsuzluk, miktar, atıf, özne ve nedensellik denetimini koru. Anlam çıkarımı yanılabilir; matematiksel eşdeğerlik iddiası yoktur.
2. **Birden fazla kuruluş üreticisi ekle.** Tanım, amaç, yöntem, karşılaştırma, isimleştirme/fiilleştirme, açık eyleyenli etken/edilgen, koşul ve bulgu anlatımı. Her aile olumlu ve anlamı bozan olumsuz örneklerle sınanmalı. Paragraf için sınırlı aday bütçesi kullan; kural sayısını kalite ölçüsü sayma.
3. **Paragraf planlayıcısı ekle.** Cümle bölme/birleştirme, göndermeler, bağlaçlar ve atıfların hangi iddiaya ait olduğunu izle. Önce/sonra cümleleri bire bir saymak yerine önerme eşlemesi kullan. Word alan haritası yeni eşlemeyi ayrıca doğrulamalı.
4. **Doğal üretim için eğitilmiş bir üretici değerlendir.** Yerel küçük model veya göreve özel encoder-decoder adaylarını aynı gizli testte karşılaştır. İsim cümleleri ve akademik açıklamalar için insan tarafından doğrulanmış Türkçe/İngilizce parafraz çiftleriyle uyarlama yap. Transformers/PEFT eğitim araçlarıdır; tek başlarına Türkçe kalitesini artırmazlar. Eğitim başka bir geliştirme makinesinde yapılabilir; 8 GB kullanıcı cihazında eğitim gerekmez. Hazır büyük üretici kullanılmayacaksa kapsamlı dil üretim ve veri çalışması gerekir; kusursuzluk garantisi yoktur.
5. **Yerel örnek belleği ve alan bilgisi ekle.** Lisansı uygun, kaynakları kayıtlı örneklerden görev/alan açısından benzer kuruluşları getir. Rekreasyon, psikoloji, hukuk ve mühendislik gibi alanların terimlerini ve açıklama örneklerini ayır. Korpus cümlesinden yeni olgu ekleme. Sentence Transformers + FAISS bunun için adaydır; benzerlik anlam eşdeğerliğini kanıtlamaz. Rastgele internet sayfalarını doğrudan kurala dönüştürme.
6. **Türkçe çözümleme ve dil bilgisini yükselt.** Stanza gibi ikinci çözümleyiciyi mevcut UDPipe ile karşılaştır; hazır modelin Türkçe desteği, dağıtım lisansı, ham metin başarısı ve belleği ölçülmeden ürün bağımlılığı yapma. Kişi/iyelik/hâl uyumu, isim tamlaması ve yan cümle bağlanması için ayrı hata korpusu oluştur. Daha büyük parser tek başına parafraz üretmez.
7. **Bağımsız kalite değerlendirmesi kur.** Eğitim dışında tutulan, farklı alan ve yapılardan en az yüzlerce metinde doğru adayın reddi, yanlış adayın kabulü, cümle/paragraf değişim kapsamı, terim korunumu, doğallık, hız ve bellek ayrı ölçülsün. İnsan değerlendirmesi olmadan “insani/mükemmel” sonucu ilan edilmesin. Kaynak metni aynen döndürmek başarı sayılmasın.

## İncelenen açık kaynak adayları

- [Stanford Stanza](https://github.com/stanfordnlp/stanza): ikinci cümle/bağımlılık çözümleyicisi adayı. Bu incelemede Türkçe ağırlıkları indirilip kıyaslanmadı; program ve ağırlık lisansları ayrı incelenmeli.
- [Sentence Transformers](https://github.com/huggingface/sentence-transformers): yerel örnek bulma ve aday sıralama araçları, Apache-2.0 program lisansı.
- [FAISS](https://github.com/facebookresearch/faiss): yerel benzerlik indeksi, MIT program lisansı.
- [Transformers](https://github.com/huggingface/transformers) ve [PEFT](https://github.com/huggingface/peft): üretici model çalıştırma/eğitim uyarlama araçları, Apache-2.0 program lisansları. Model ağırlığının lisansı ve Türkçe yeteneği ayrıca doğrulanmalı.

Bu adayların GitHub depoları incelendi; paketle birlikte eklenmiş gibi sunulmaz. 8 GB RAM için modeller aynı anda yüklenmek yerine sırayla çalıştırılmalı; toplam Word + işletim sistemi belleği gerçek kullanıcı cihazında ölçülmelidir. Kullanıcı metnini sunucuya gönderen yeni bir yol eklenmedi.
