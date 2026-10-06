# Akademik Parafraz

Bu ilk Windows kurulum paketi oluşturulmuş ve otomatik motor testlerinden geçmiştir. Gerçek Word içindeki kurulum, biçim, bağlantı ve geri alma kabulü henüz tamamlanmamıştır. Paket geliştirme yayıncı sertifikası kullanır; kurulumda yayıncı/güven sorusu görülebilir.

## Kurulum

Windows 10/11 x64 ve masaüstü Word 2016 veya daha yeni bir sürüm gerekir. Word 32 veya 64 bit olabilir. Açık belgelerinizi kaydedip Word'ü kapatın ve `AkademikParafraz-1.0.0-Setup.exe` dosyasını açın. Kurulum eksik .NET Framework 4.8, Word veya Microsoft VSTO Runtime bileşenini bildirirse önce o bileşeni tamamlayın. VSTO Runtime'ın resmi indirmesi: https://www.microsoft.com/en-us/download/details.aspx?id=105522

Java, NLP motoru ve sözlük pakete dahildir. Visual Studio, SDK, Maven veya ayrıca Java kurmanız gerekmez. Yayıncı sertifikası otomatik güvenilir yapılmaz; kurumsal bilgisayarınız kurulumu engellerse kurumunuzun yazılım yöneticisiyle ilerleyin.

## Kullanım

1. Word'ü açın ve bir cümle, paragraf veya birkaç paragraf seçin.
2. **AKADEMİK PARAFRAZ > Orta > Parafraz Et** komutunu kullanın.
3. Önizlemede öneriyi okuyun; gerekirse sonraki alternatife geçin.
4. **Uygula** öneriyi belgeye yazar. **İptal** belgeyi değiştirmez.
5. Son işlemi **Ctrl+Z** veya eklentinin **Geri Al** komutuyla geri alın.

Koruma seçenekleri varsayılan olarak açıktır. Bağlantının görünen metnini değiştirmek için **Bağlantıları Koru** seçeneğini kapatın; URL hedefini değiştirmek amaçlanmaz. **Değişiklikleri İzleyerek Parafraz Et** seçeneği Word'ün doğal değişiklik izleme özelliğini kullanır. Korunmasını istediğiniz özel ifadeyi seçip **Terimi Kilitle** komutuyla sözlüğünüze ekleyin.

## İnternet ve kapsam

Parafraz yerelde çalışır; LLM veya ücretli API anahtarı kullanılmaz. İnternet varsayılan olarak kapalıdır. İsteğe bağlı sözlük sorgusunda yalnızca sözlük ekranına yazdığınız tek kelime gönderilir. Bu sürüm akademik Türkçe içindir; İngilizce çeviri/parafraz içermez. Güvenli öneri bulunmayan ifadeler değişmeden kalır. Öneriler sınırlı kurallara dayanır; metnin anlamını uygulamadan önce okuyun.

## Word doğrulaması

Kurulumdan sonra Word'ü kapatıp kurulum ZIP'indeki `Verify-Word.cmd` dosyasını açabilirsiniz. Test yeni, kaydedilmeyen bir Word belgesi kullanır; atıf, istatistik, teknik terim, font/paragraf biçimi, hyperlink hedefi, tek Undo ve Track Changes kontrollerini çalıştırır. Ayarlarınıza geri döner ve `Word-Acceptance-Report.json` dosyasını oluşturur. Açık Word belgelerini kendiliğinden kapatmaz. Bu sonuç, geniş Word 32/64 bit kabul matrisinin tamamı değildir.

Eklenti normal Windows **Yüklü Uygulamalar** ekranından kaldırılır. Kişisel sözlük, ayar ve geçmiş verileri `%LOCALAPPDATA%\AkademikParafraz` altında saklanır.
