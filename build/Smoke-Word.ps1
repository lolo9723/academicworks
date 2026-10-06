$ErrorActionPreference='Stop'
if(Get-Process WINWORD -ErrorAction SilentlyContinue){throw 'Bu test yeni bir Word oturumu açar. Önce açık Word belgelerinizi kaydedip Wordü kapatın.'}
$word=New-Object -ComObject Word.Application
$word.Visible=$true
$doc=$null;$api=$null;$originalTracking=$false;$originalLinks=$true;$originalStrength=2
try{
 $addin=$word.COMAddIns.Item('AcademicParaphraser.WordAddin')
 $addin.Connect=$true
 $api=$addin.Object
 if(-not $api){throw 'VSTO otomasyon arayüzü açılmadı; kurulum doğrulanamadı.'}
 $originalTracking=$api.Tracking;$originalLinks=$api.LinksProtected;$originalStrength=$api.DefaultStrength
 $doc=$word.Documents.Add()
 $source='Doğayla ilişkinin öznel zindelik üzerindeki etkisinin anlamlı olduğu belirlenmiştir (Yılmaz & Demir, 2024; β=.43, p<.001).'
 $rest='Önem arz etmektedir.'
 $doc.Content.Text=$source+"`r"+$rest+"`r"+'tespit edilmiştir.'+"`r"
 $linkStart=$source.Length+1+$rest.Length+1
 $linkRange=$doc.Range($linkStart,$linkStart+17)
 $link=$doc.Hyperlinks.Add($linkRange,'https://example.org/protected','','','tespit edilmiştir','')
 $linkUrl=$link.Address
 $api.SetLinks($true)
 $selection=$doc.Range(0,$source.Length)
 $selection.Font.Name='Arial';$selection.Font.Size=12;$selection.Font.Italic=1
 $paragraph=$selection.ParagraphFormat
 $paragraph.Alignment=3;$paragraph.FirstLineIndent=18;$paragraph.SpaceAfter=6
 $selection.Select()
 $before=$doc.Content.Text
 $api.SetTracking($false);$api.Generate(2)
 $deadline=(Get-Date).AddSeconds(120)
 while($api.State -eq 'busy' -and (Get-Date) -lt $deadline){Start-Sleep -Milliseconds 200}
 if($api.State -ne 'ready'){throw "Öneri üretilmedi: $($api.State)"}
 if($api.Apply() -ne 'applied'){throw 'Öneri uygulanamadı.'}
 $after=$doc.Content.Text
 if($doc.Hyperlinks.Count -ne 1 -or $doc.Hyperlinks.Item(1).Address -ne $linkUrl){throw 'Korunan hyperlink hedefi değişti.'}
 if($after -eq $before){throw 'Metin dönüşmedi.'}
 foreach($protected in @('(Yılmaz & Demir, 2024; β=.43, p<.001)','öznel zindelik','Önem arz etmektedir.')){if(-not $after.Contains($protected)){throw "Korunan bölüm değişti: $protected"}}
 $r=$doc.Paragraphs.Item(1).Range
 if($r.Font.Name -ne 'Arial' -or $r.Font.Size -ne 12 -or $r.Font.Italic -ne 1 -or $r.ParagraphFormat.Alignment -ne 3){throw 'Word biçimi değişti.'}
 $doc.Undo(1) | Out-Null
 if($doc.Content.Text -ne $before){throw 'Tek Undo ilk metni geri getirmedi.'}
 $doc.Range(0,$source.Length).Select();$api.SetTracking($true);$api.Generate(2)
 $deadline=(Get-Date).AddSeconds(120)
 while($api.State -eq 'busy' -and (Get-Date) -lt $deadline){Start-Sleep -Milliseconds 200}
 if($api.State -ne 'ready' -or $api.Apply() -ne 'applied'){throw 'Değişiklikleri İzle işlemi başarısız.'}
 if($doc.Revisions.Count -lt 1){throw 'Word doğal revisions üretmedi.'}
 $doc.Undo(1) | Out-Null
 $doc.Hyperlinks.Item(1).Range.Select();$api.SetTracking($false);$api.Generate(2)
 $deadline=(Get-Date).AddSeconds(120)
 while($api.State -eq 'busy' -and (Get-Date) -lt $deadline){Start-Sleep -Milliseconds 200}
 if($api.State -ne 'idle'){throw 'Varsayılan link koruması düzenlenebilir öneri üretti.'}
 $api.SetLinks($false);$doc.Hyperlinks.Item(1).Range.Select();$api.Generate(2)
 $deadline=(Get-Date).AddSeconds(120)
 while($api.State -eq 'busy' -and (Get-Date) -lt $deadline){Start-Sleep -Milliseconds 200}
 if($api.State -ne 'ready' -or $api.Apply() -ne 'applied'){throw 'Hyperlink görünen metni değiştirilemedi.'}
 if($doc.Hyperlinks.Count -ne 1 -or $doc.Hyperlinks.Item(1).Address -ne $linkUrl -or $doc.Hyperlinks.Item(1).Range.Text -eq 'tespit edilmiştir'){throw 'Görünen link metni/URL kontrolü başarısız.'}
 $doc.Undo(1) | Out-Null
 Write-Host 'PASS: Word yükleme, metin/atıf/terim koruma, font/paragraf biçimi, hyperlink/URL, tek Undo ve Track Changes.' 
}finally{
 try{if($api){$api.SetTracking($originalTracking);$api.SetLinks($originalLinks);$api.SetStrength($originalStrength)}}catch{}
 if($doc){$doc.Close(0)}
 $word.Quit()
 [Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
