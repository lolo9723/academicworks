# Serbest yeniden yazım için geliştirme gereksinimleri

Bu tasarımın yerel model + geliştirilen motor yönü, sonraki kullanıcı yönlendirmesiyle 1.5.0 çalışmasına alınmıştır. Güncel uygulama ve sınırlar için [Local-Engine-1.5.0.md](Local-Engine-1.5.0.md) belgesine bakın. Aşağıdaki maddeler gereksinim geçmişidir; tamamlanma kanıtı değildir.

## İstenen davranış

Cümlenin ifade ettiği düşünceyi, özneyi ve neden ilişkisini koruyarak doğal bir anlatım üretmek. Amaç yalnızca sözcük seçimi veya dizilişi değildir. Ayrı bir talep olmadıkça yeni davranış, sonuç, veri veya gerekçe eklenmemeli.

Anlamı koruyan parafraz ile yorum içeren genişletme ayrı işlemlerdir. Yorum içeren genişletme varsayılan olmayacak. Akademik parafrazda metinde söylenmeyen davranışları eklemek başarılı bir yeniden yazım sayılmayacak.

## Önceki kısıt ile yeni ihtiyaç

Gönderilen şartname, herhangi bir LLM veya üretken yapay zekâ kullanılmasını açıkça yasaklıyor. Serbest üretim için yerel dil modeli seçeneği bu koşulun değiştirilmesini gerektirir. Sözlükleri ve sonlu kuralları büyütmek sınırsız, insan düzeyinde yeniden anlatım garantisi sağlamaz. Bir dil modeli de kusursuzluk garantisi değildir.

Yerel bir model seçilirse ücretli bir API anahtarı gerekmeyebilir. Yerel model sunucusuna yapılan program çağrısı teknik olarak bir API'dir; ücretli bulut hizmeti veya belgeyi internete gönderme zorunluluğu anlamına gelmez. Model dosyasını ilk indirme işlemi internete bağlı olabilir; üretim bilgisayarın içinde yapılabilir.

## Entegrasyon koşulları

1. Mevcut Word seçim yakalama, biçim sınırları, alanlar ve hyperlink korumaları korunacak. Modelin çıktısı doğrudan tüm Word seçiminin üzerine yazılmayacak.
2. Özel isimler, teknik terimler, atıflar, sayılar, birimler ve bağlantı alanları korunacak. Model bu alanları düşürür, çoğaltır, başka bir role taşır veya koruma eşlemesi bozulursa öneri reddedilecek.
3. Üretimden sonra yeni bir eylem veya sav eklenmesi, özne değişmesi, olumsuzluk, koşul, zaman, neden ilişkisi, kesinlik ve kapsam değişimleri denetlenecek. Mevcut morfem sayacı bu gereksinimi tek başına karşılamıyor.
4. Otomatik bir denetimin geçmesi anlam eşdeğerliği ispatı olarak gösterilmeyecek. Kullanıcı önizlemede metni okuyacak ve uygulama kararı verecek.
5. İptal, sınırlı bağlam/bellek, süre aşımı ve başarısız üretimde belgeyi değiştirmeme davranışı olacak. İnternet sözlükleri üretim motorunun yerini tutan kaynaklar olarak sunulmayacak.

## 8 GB RAM için sınama

Küçük ve nicemlenmiş bir model aday olabilir; model ve nicemleme biçimi henüz seçilmedi. Word açıkken toplam RAM, işlem süresi, Türkçe dil doğallığı ve kaynak/çıktı anlam uyumu ölçülmeden uygunluk iddia edilmeyecek. Dahili ekran kartıyla GPU hızlandırması varsayılmayacak.

Entegrasyon uygulanırsa, kullanıcının örneklerini ezberleyen dönüşümlerle başarı gösterilmeyecek. En az beş farklı alanda bağımsız metinler ve özellikle yeni bilgi ekleme, yanlış neden ilişkisi, kişi değişimi, olumsuzluk ve genelleme tuzaklarıyla değerlendirme yapılacak. Kaynak metinler, gerçek motor çıktıları, kabul/red nedenleri ve insan tarafından yapılan metin incelemesi raporlanacak. Canlı Word üzerinde biçim, bağlantı, Undo ve Track Changes kabulü ayrıca gereklidir.
