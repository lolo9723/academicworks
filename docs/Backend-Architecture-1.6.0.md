# 1.6.0 ayrı backendler

Bu sürüm, hazır büyük dil modeli çalıştırmadan cümle kuruluşu üretir. Genel ve kusursuz anlam çözümü sağlandığı iddia edilmez. Teknik testler, öğrenilmiş çözümleyicinin test başarısı ve gerçek parafraz çıktısı ayrı raporlanır. Sonuçlar tamamlanınca kabul edilen ve elenen öneriler aynı korpusla yayımlanır.

| Aşama | Uygulama | Ayrı kaynak | Görev ve sınır |
|---|---|---|---|
| Sözcük/çekim | Zemberek 0.17.1 | [ahmetaa/zemberek-nlp](https://github.com/ahmetaa/zemberek-nlp) | Türkçe çözümleme ve çekim; genel anlam anlayıcısı değildir. |
| Cümle çözümleme | UDPipe 1.3.0 + burada eğitilmiş küçük çözümleyiciler | [ufal/udpipe](https://github.com/ufal/udpipe/tree/v1.3.0), [Turkish-BOUN](https://github.com/UniversalDependencies/UD_Turkish-BOUN/tree/5cb7559d4efd63c22d253ff9e3fa2b0202b7913e), [English-EWT](https://github.com/UniversalDependencies/UD_English-EWT/tree/c5baffde1e106bcd828c520109eb905bfc3ac06f) | Sözcükler arası baş/bağımlılık, özne, nesne ve yan cümle etiketleri. Yanlış çözümleme olabilir. |
| Kuruluş üretimi | DependencyConstructionBackend | Bu projenin `Core/Backends/ConstructionBackend.cs` dosyası | Bütün öbekleri taşır; Türkçede nesne/önerme/bağlı tümleç odağı, İngilizcede basit geçmiş etken-edilgen ve sınırlı neden cümlesi yerleşimi. Her işlem yeniden uygulanabilir kayıt üretir. Eş anlamlı sözcükleri tek tek değiştirmez. |
| Anlam denetimi | DerivationMeaningBackend | Bu projenin `Core/Backends/EvaluationBackends.cs` dosyası | Kuruluşu bağımsız tekrarlar; sayılar, birimler, olumsuzluk, kip, zaman, eylem kökleri ve bağımlılık rollerini karşılaştırır. Bağımlılık çözümlemesi veya işlem izi anlam eşdeğerliği kanıtı değildir. |
| Dil bilgisi | SeparateGrammarBackend | [languagetool-org/languagetool](https://github.com/languagetool-org/languagetool/tree/v6.6), Zemberek ve burada yazılan Türkçe kontroller | LanguageTool İngilizceyi yerelde denetler; Türkçeyi destekliyormuş gibi kullanılmaz. Türkçe denetimi bilinen morfoloji ve sınırlı açık zamir-kişi uyumudur, tam dil bilgisi düzelticisi değildir. |
| Nihai karar | IndependentFinalEvaluationBackend | Bu projenin `Core/Backends/EvaluationBackends.cs` dosyası | Yeni dil bilgisi sorunları, yeterli kuruluş farkı ve anlam kontrolünü birlikte değerlendirir. Sahte anlam doğruluğu yüzdesi göstermez. |
| Word eşleme | WordProposalCompiler | Bu projenin `Core/Backends/WordProposalCompiler.cs` dosyası | Her değişen cümleyi gerçek düzenlenebilir Word alanlarına kesin eşler. Biçim/bağlantı sınırını aşan öneri elenir; başka cümlenin biçim sınırı uygun dönüşümü engellemez. |

Arayüzler `ILinguisticBackend`, `IConstructionBackend`, `IMeaningBackend`, `IGrammarBackend`, `IFinalEvaluationBackend` ayrı değiştirilebilir. Kullanılan üçüncü taraf kaynaklar kendi GitHub projeleridir; burada yazılmış backendler tek uygulama deposunda bağımsız modüllerdir. GitHub deposunu çoğaltmak dil bilgisini veya anlam gücünü kendiliğinden artırmaz.

## Gerçek eğitim

Hazır UDPipe dil modellerinin lisansı non-commercial koşulu içerdiğinden bunlar pakete konulmaz. Kullanılan program değiştirilmemiş MPL-2.0 UDPipe'dır. Turkish-BOUN ve English-EWT UD 2.17 verisi CC BY-SA 4.0'dır; türetilen küçük modeller aynı atıf/paylaşım koşullarıyla dağıtılır. Ham veri commit'leri ve SHA256 özetleri `build/dependency-backend.json` içinde sabittir.

`python build/Prepare-DependencyBackend.py` tokenizasyon, sözcük türü/çekim, kök ve bağımlılık ağacı eğitir. Kullanıcı metni eğitimde kullanılmaz. Eğitim verisi, geliştirme verisi ve test verisi ayrıdır. Parafrazın beş sentetik paragrafı eğitime eklenmez. Test ölçümü altın tokenizasyon üzerinde etiketleme/ağaç başarısıdır; Word'deki gerçek ham metin veya anlam doğruluğu yüzdesi değildir. Gerçek ham metin parafrazı `AcademicParaphraser.BackendProbe` ile ayrıca sınanır.

Bu eğitim büyük dil modeli veya insan üslubu eğitimi değildir. Modeli üreten komutlar, parametreler, veri özetleri, model özetleri ve test çıktıları paketle birlikte kaydedilir. Aynı veride hızlı prototip eğitimi, en iyi yayımlanmış çözümleyici başarısını sağlamış sayılmaz.

## Kaynak ve süreç sınırları

Bileşenler yerelde çalışır; API anahtarı, buluta belge gönderme ve ekran kartı gerekmez. Java en fazla 512 MB heap ile çağrılır; gerçek işletim sistemi çalışma belleği daha yüksek olabilir. Çözümleyici küçük modelini işlem başına yükler ve kapatır. İngilizce dil bilgisi Java işlemi gerektiğinde açılır; iptal/eklenti kapanışı sahip olunan işlemleri kapatır. Eğitim ayrı geliştirme/CI işlemidir; kullanıcının bilgisayarında yapılmaz. Paket eğitimli küçük modelleri içerir; 2,5 GB LLM indirmesi bu motor için gerekmez.

En fazla 2.400 karakterlik paragraflar ve altı kuruluş seçeneği sınırı korunur. Odak parçacıkları, alıntılar, koordinasyon veya çözümlenemeyen Word sınırları dönüşümü azaltabilir. Kapsam dışındaki cümle için serbestçe metin üretme veya internetten otomatik sınırsız öğrenme yoktur. Üretim ve bağımsız tekrar çözümlemenin uyuşmazlığı doğru bir parafrazı da eleyebilir. Nihai önizleme okunmalıdır. Gerçek Word/8 GB kullanıcı makinesi kabulü henüz yapılmadı.
