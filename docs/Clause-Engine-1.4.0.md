# Cümle ve yan cümle motoru 1.4.0

Yeni sürüm, eş anlamlı kelime seçmek yerine bağlama göre çekim üreten yan cümle dönüşümleri ekler. En önemli düzeltme, ilk büyük harfli kelimeyi metnin sonundaki küçük harfli tekrarına eşleyen NLP konum hatasının giderilmesidir. Bu hata kelime çözümlemesinin büyük bölümünün atlanmasına yol açabiliyordu.

## Sabit beş alan sınaması

Metinler halka açık, sentetik örneklerdir. Kullanıcının diş hekimliği paragrafı bu sınamada kullanılmadı. Hukuk, mühendislik, ekonomi, eğitim ve çevre bilimlerinden beş metin ilk çalıştırmadan önce sabitlendi; geliştirme boyunca metinler değiştirilmedi. Korpus SHA256: `49c45d2b010fa0716d8ff3a8aa950335fe14b09db4c582a52a0591bc42601870`.

Bu bir işlev ve sorun bulma sınamasıdır; geniş bir yazar örneklemi veya kör kalite değerlendirmesi değildir. Metinlerde ortak cümle kuruluşları da vardır. Cümle değişimi, anlam doğruluğu veya insan yazımı yüzdesi olarak yorumlanamaz.

| Alan | 1.3.0 değişen kaynak cümlesi | 1.4.0 değişen kaynak cümlesi |
|---|---:|---:|
| Hukuk | 0/5 | 4/5 |
| Mühendislik | 1/5 | 4/5 |
| Ekonomi | 0/5 | 4/5 |
| Eğitim | 0/5 | 4/5 |
| Çevre bilimleri | 0/5 | 4/5 |

1.3.0 karşılaştırması yayımlanan kaynak commitinden ayrı çalışma kopyası oluşturularak, o sürümün kendi Java motoruyla yapıldı. Aynı beş metinde 1.3.0 toplam 1/25, yeni motor 20/25 kaynak cümlesinde dönüşüm üretti. Beş karmaşık son cümle değişmeden kaldı. Yeni çıktılarda eş anlamlı kelime editleri uygulanmadı; atıf/isim/sayı korumaları, seçili terimler ve olumsuzluk/olasılık/koşul sinyalleri kontrol edildi. Sıradan bir terimin yeni cümle başında büyük harfle başlamasına izin verildi; gerçek korunan alanlara edit uygulanmadığı ayrıca doğrulandı.

## Bulunan ve düzeltilen sorunlar

1. Büyük/küçük harf tekrarında atlanan kelimeler: çözümleyicinin gerçek token girdisi kullanılıyor ve ilk uygun konum seçiliyor.
2. Önceki cümleyi içine alan karşıtlık kalıbı: tek kaynak cümle sınırı uygulanıyor; nokta, sayı ve kısaltmalar ayrılıyor.
3. Ek ve çatı hataları: isim-fiil, tamlayan ve edilgen biçimler Zemberek üreticisinden alınıyor. Cümlede seçilen kök ve morfem okuması korunuyor; örneğin “doğrulamamıştır”ın yanlış “doğrulmak/başaramamak” okumasına geçilmiyor.
4. “İncelenilmesi” gibi uzamış yüzeyler: aynı morfolojik okumanın kısa standart biçimi tercih ediliyor; ayrı anlam okumaları birbirinin yerine seçilmiyor.
5. “Çalışma/inceleme” sözcüğünün olumsuz emir sanılması: belirsiz emir okuması otomatik anlam-kaybı kanıtı sayılmıyor. “İse” bağlacının koşul eki sayılması da ayrılıyor.
6. “Deneysel olarak, bu çalışmada” gibi ağır sıralama: bağlam taşınırken tarz belirteci yüklem yanında tutuluyor.
7. Uzun süren kişisel regex: o kural eleniyor; diğer cümle önerileri sürdürülebiliyor.

## Gerçek öncesi/sonrası çıktıları

### Hukuk

**Kaynak:**

İdari yaptırımların hukuka uygunluğu, ölçülülük ilkesi bakımından incelenmiştir. Bulgular, gerekçesi açıklanmayan kararların daha sık iptal edildiğini göstermektedir. İncelemede 48 karar ele alınmış olmakla birlikte, bunların yalnızca 11'i iptal edilmiştir. Ankara İdare Mahkemesi arşivinde tarafların savunmaları karşılaştırılırken, delillerin sunuluş sırası ayrıca kaydedilmiştir. Bir kararın iptal edilmiş olması, benzer her uyuşmazlığın aynı biçimde sonuçlanacağını kanıtlamaz.

**Motorun ilk önerisi:**

İdari yaptırımların hukuka uygunluğu üzerinde ölçülülük ilkesi bakımından inceleme yapılmıştır. Gerekçesi açıklanmayan kararların daha sık iptal edildiği, bulgularla gösterilmektedir. İncelemede 48 karar ele alınmıştır; bununla birlikte bunların yalnızca 11'i iptal edilmiştir. Ankara İdare Mahkemesi arşivinde tarafların savunmalarının karşılaştırılması sırasında, delillerin sunuluş sırası ayrıca kaydedilmiştir. Bir kararın iptal edilmiş olması, benzer her uyuşmazlığın aynı biçimde sonuçlanacağını kanıtlamaz.

**Editoryal inceleme:** İptal sayısı ve kurum adı korunuyor. “Her uyuşmazlık aynı sonuçlanır” biçiminde bir genelleme eklenmedi. İlk cümledeki nominal anlatım doğru olmakla birlikte ağırdır.

**Değişmeden kalan:** Bir kararın iptal edilmiş olması, benzer her uyuşmazlığın aynı biçimde sonuçlanacağını kanıtlamaz.

### Mühendislik

**Kaynak:**

Bu çalışmada kompozit kirişlerin yorulma davranışı deneysel olarak incelenmiştir. Ölçüm sonuçları, lif oranının artırılmasının her durumda dayanımı yükseltmediğini göstermektedir. Numunelerin yüzeyleri incelenirken, çatlakların başlangıç konumları kaydedilmiştir. Deneyler 25 °C sıcaklıkta ve 12 kN yük altında yürütülmüş olmakla birlikte, saha koşulları ayrıca değerlendirilmemiştir. Ortaya çıkan farklar üretim toleranslarıyla ilişkili olabilir; bu nedenle tek bir ölçümden kesin bir tasarım sınırı çıkarılmamalıdır.

**Motorun ilk önerisi:**

Kompozit kirişlerin yorulma davranışı bu çalışmada deneysel olarak incelenmiştir. Lif oranının artırılmasının her durumda dayanımı yükseltmediği, ölçüm sonuçlarıyla gösterilmektedir. Numunelerin yüzeylerinin incelenmesi sırasında, çatlakların başlangıç konumları kaydedilmiştir. Deneyler 25 °C sıcaklıkta ve 12 kN yük altında yürütülmüştür; bununla birlikte saha koşulları ayrıca değerlendirilmemiştir. Ortaya çıkan farklar üretim toleranslarıyla ilişkili olabilir; bu nedenle tek bir ölçümden kesin bir tasarım sınırı çıkarılmamalıdır.

**Editoryal inceleme:** Olumsuz bulgu, 25 °C, 12 kN ve “olabilir” ifadesi korunuyor. Deneysel yöntemin belirteci yüklem yanında tutuldu; tasarım sınırı konusunda kesinlik eklenmedi.

**Değişmeden kalan:** Ortaya çıkan farklar üretim toleranslarıyla ilişkili olabilir; bu nedenle tek bir ölçümden kesin bir tasarım sınırı çıkarılmamalıdır.

### Ekonomi

**Kaynak:**

Hanehalklarının tüketim harcamaları, gelir grupları açısından karşılaştırılmıştır. Veriler, fiyat artışlarının düşük gelirli haneleri daha fazla etkilediğini göstermektedir. Örneklemde 620 hane bulunmuş olmakla birlikte, kayıt dışı gelirler doğrudan ölçülmemiştir. Tüketim düzeyi hesaplanırken, eşdeğer hane büyüklüğü dikkate alınmıştır. Gözlenen ilişkinin nedensel bir etkiyi kanıtladığı söylenemez; sonuçlar yalnızca incelenen döneme ilişkin bir değerlendirme sunmaktadır.

**Motorun ilk önerisi:**

Hanehalklarının tüketim harcamaları üzerinde gelir grupları açısından karşılaştırma yapılmıştır. Fiyat artışlarının düşük gelirli haneleri daha fazla etkilediği, verilerle gösterilmektedir. Örneklemde 620 hane bulunmuştur; bununla birlikte kayıt dışı gelirler doğrudan ölçülmemiştir. Tüketim düzeyinin hesaplanması sırasında, eşdeğer hane büyüklüğü dikkate alınmıştır. Gözlenen ilişkinin nedensel bir etkiyi kanıtladığı söylenemez; sonuçlar yalnızca incelenen döneme ilişkin bir değerlendirme sunmaktadır.

**Editoryal inceleme:** 620 hane ve ölçülmeyen kayıt dışı gelir ayrımı korunuyor. İlişki nedensellik kanıtı olarak sunulmadı. İlk cümle daha akıcı bir insan redaksiyonuna ihtiyaç duyabilir.

**Değişmeden kalan:** Gözlenen ilişkinin nedensel bir etkiyi kanıtladığı söylenemez; sonuçlar yalnızca incelenen döneme ilişkin bir değerlendirme sunmaktadır.

### Eğitim

**Kaynak:**

Öğrencilerin okuduğunu anlama becerileri, öğretim yöntemleri bakımından değerlendirilmiştir. Araştırma bulguları, geribildirimin bütün öğrencilerde aynı düzeyde etkili olmadığını göstermektedir. Uygulama 8 hafta sürmüş olmakla birlikte, katılımcılar gruplara rastgele atanmamıştır. Sınav sonuçları karşılaştırılırken, başlangıç puanları dikkate alınmıştır. Mavişehir Ortaokulu öğrencilerinden elde edilen sonuçların diğer okullara doğrudan genellenmesi uygun olmayabilir.

**Motorun ilk önerisi:**

Öğrencilerin okuduğunu anlama becerileri üzerinde öğretim yöntemleri bakımından değerlendirme yapılmıştır. Geribildirimin bütün öğrencilerde aynı düzeyde etkili olmadığı, araştırma bulgularıyla gösterilmektedir. Uygulama 8 hafta sürmüştür; bununla birlikte katılımcılar gruplara rastgele atanmamıştır. Sınav sonuçlarının karşılaştırılması sırasında, başlangıç puanları dikkate alınmıştır. Mavişehir Ortaokulu öğrencilerinden elde edilen sonuçların diğer okullara doğrudan genellenmesi uygun olmayabilir.

**Editoryal inceleme:** Rastgele atama yapılmadığı ve farklı okullara genellemenin belirsizliği korunuyor. Okul adı değişmedi. “Bütün öğrenciler”in kapsamı daraltılmadı.

**Değişmeden kalan:** Mavişehir Ortaokulu öğrencilerinden elde edilen sonuçların diğer okullara doğrudan genellenmesi uygun olmayabilir.

### Çevre bilimleri

**Kaynak:**

Toprak nemi, bitki örtüsü bakımından incelenmiştir. Sonuçlar, yüzey sıcaklığının kurak dönemde daha hızlı yükseldiğini göstermektedir. Ölçümler 36 istasyonda yapılmış olmakla birlikte, yüksek rakımlı alanlar örnekleme dahil edilmemiştir. Nem düzeyi ölçülürken, sensörlerin kalibrasyon kayıtları ayrıca kontrol edilmiştir. Yağış miktarı değişirse gözlenen örüntü farklılaşabilir; mevcut veriler, bütün bölgelerde aynı sonucun ortaya çıkacağını göstermemektedir.

**Motorun ilk önerisi:**

Toprak nemi üzerinde bitki örtüsü bakımından inceleme yapılmıştır. Yüzey sıcaklığının kurak dönemde daha hızlı yükseldiği, sonuçlarla gösterilmektedir. Ölçümler 36 istasyonda yapılmıştır; bununla birlikte yüksek rakımlı alanlar örnekleme dahil edilmemiştir. Nem düzeyinin ölçülmesi sırasında, sensörlerin kalibrasyon kayıtları ayrıca kontrol edilmiştir. Yağış miktarı değişirse gözlenen örüntü farklılaşabilir; mevcut veriler, bütün bölgelerde aynı sonucun ortaya çıkacağını göstermemektedir.

**Editoryal inceleme:** Örnekleme alınmayan yüksek rakımlı alanlar, koşul ve olumsuz genelleme korunuyor. Son karmaşık cümle değişmeden kaldı.

**Değişmeden kalan:** Yağış miktarı değişirse gözlenen örüntü farklılaşabilir; mevcut veriler, bütün bölgelerde aynı sonucun ortaya çıkacağını göstermemektedir.

## Sınırlar ve doğrulama

Kaynakların rollerinin, bağlamın ve vurgu farklarının tümünü kanıtlayan bir semantik model yoktur. Sinyal sayacı gerekli bir kontroldür, anlam eşdeğerliği ispatı değildir. Üretilen metinler okunarak kontrol edildi; bazı nominal biçimler ağır ve benzer üsluptadır. Tek bir güvenli kalıp varsa üç farklı alternatif zorla üretilmez. Her paragrafı serbestçe yeniden kurma, İngilizce çeviri/parafraz veya kullanıcının üslubunu öğrenme eklenmedi.

LLM etkinleştirilmedi; paragraf internete gönderilmedi. Sözlük ve denetlenmiş internet yapı bankası mevcut kaynaklarla devam eder. Eklenen 126 yerel şema az sayıda incelenmiş dilbilgisi ailesinin biçimleridir; 126 bağımsız sentaks veya yüz binlerce insan kalitesinde kural değildir.

Yerel Release motor sınaması: **152/152**, sıfır hata ve sıfır atlanan test. Gerçek WordAddin kaynakları ve bildirilmiş referanslar derlendi. Windows kurulum ve CI doğrulaması paket oluşturulurken ayrıca tamamlanacaktır. Canlı Word belge kabulü bu ortamda yapılmadı. 8 GB kullanıcının bilgisayarındaki performans ölçülmedi; Java heap sınırı 512 MiB kalır.

Tekrar üretim: `dotnet run --project tools/AcademicParaphraser.QualityProbe -c Release -- <kenet.sqlite> <java> <nlp.jar> <rapor-klasörü> --domains`. Ham sonuçlar `domain-verification.json` içine yazılır. Korpus: [multidomain-v1.json](../tests/fixtures/multidomain-v1.json).
