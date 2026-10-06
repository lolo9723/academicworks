param([string]$ReportPath=(Join-Path $PSScriptRoot 'Word-Acceptance-Report.json'))
$ErrorActionPreference='Stop'
if(Get-Process WINWORD -ErrorAction SilentlyContinue){throw 'Bu test yeni bir Word oturumu açar. Önce açık Word belgelerinizi kaydedip Wordü kapatın.'}
$word=New-Object -ComObject Word.Application
$word.Visible=$true
$doc=$null;$api=$null;$originalTracking=$false;$originalLinks=$true;$originalStrength=2
$report=[ordered]@{ utc=(Get-Date).ToUniversalTime().ToString('o'); wordVersion=$word.Version; officeBitness=$null; productVersion='1.0.0'; passed=$false; checks=@(); error=$null }
function Read-Format($range) {
 $font=$range.Font;$paragraph=$range.ParagraphFormat
 [ordered]@{ name=$font.Name; size=$font.Size; bold=$font.Bold; italic=$font.Italic; underline=$font.Underline; color=$font.Color; superscript=$font.Superscript; subscript=$font.Subscript; alignment=$paragraph.Alignment; firstLineIndent=$paragraph.FirstLineIndent; leftIndent=$paragraph.LeftIndent; rightIndent=$paragraph.RightIndent; spaceBefore=$paragraph.SpaceBefore; spaceAfter=$paragraph.SpaceAfter; lineSpacing=$paragraph.LineSpacing; lineSpacingRule=$paragraph.LineSpacingRule }
}
function Wait-Proposals {
 $deadline=(Get-Date).AddSeconds(120)
 while($api.State -eq 'busy' -and (Get-Date) -lt $deadline){Start-Sleep -Milliseconds 200}
 if($api.State -eq 'busy'){throw 'Yerel NLP işlemi 120 saniye sınırını aştı.'}
}

try{
 $reader=New-Object IO.BinaryReader([IO.File]::OpenRead((Join-Path $word.Path 'WINWORD.EXE')))
 try{
  if($reader.ReadUInt16() -ne 0x5a4d){throw 'Word yürütülebilir dosyasının PE başlığı okunamadı.'}
  $reader.BaseStream.Position=0x3c;$pe=$reader.ReadInt32();$reader.BaseStream.Position=$pe
  if($reader.ReadUInt32() -ne 0x00004550){throw 'Word PE imzası geçersiz.'}
  $machine=$reader.ReadUInt16()
  $report.officeBitness=switch($machine){0x14c {'x86'} 0x8664 {'x64'} 0xaa64 {'ARM64'} default {throw 'Word işlem mimarisi tanınmadı.'}}
 }finally{$reader.Dispose()}
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
 $formatBefore=Read-Format $selection | ConvertTo-Json -Compress
 $selection.Select()
 $before=$doc.Content.Text
 $api.SetTracking($false);$api.Generate(2)
 Wait-Proposals
 if($api.State -ne 'ready'){throw "Öneri üretilmedi: $($api.State)"}
 if($api.Apply() -ne 'applied'){throw 'Öneri uygulanamadı.'}
 $after=$doc.Content.Text
 if($doc.Hyperlinks.Count -ne 1 -or $doc.Hyperlinks.Item(1).Address -ne $linkUrl){throw 'Korunan hyperlink hedefi değişti.'}
 if($after -eq $before){throw 'Metin dönüşmedi.'}
 foreach($protected in @('(Yılmaz & Demir, 2024; β=.43, p<.001)','öznel zindelik','Önem arz etmektedir.')){if(-not $after.Contains($protected)){throw "Korunan bölüm değişti: $protected"}}
 $r=$doc.Paragraphs.Item(1).Range
 # The paragraph mark was never assigned the selected text's font.
 $r.End=$r.End-1
 $formatAfter=Read-Format $r | ConvertTo-Json -Compress
 if($formatAfter -ne $formatBefore){throw 'Word font/paragraf biçimi değişti.'}
 $report.checks+=@('citation-number-term-outside-selection','font-paragraph-properties','protected-link-target')
 $doc.Undo(1) | Out-Null
 if($doc.Content.Text -ne $before){throw 'Tek Undo ilk metni geri getirmedi.'}
 $report.checks+='single-undo'
 $doc.Range(0,$source.Length).Select();$api.SetTracking($true);$api.Generate(2)
 Wait-Proposals
 if($api.State -ne 'ready' -or $api.Apply() -ne 'applied'){throw 'Değişiklikleri İzle işlemi başarısız.'}
 if($doc.Revisions.Count -lt 1){throw 'Word doğal revisions üretmedi.'}
 $report.checks+='native-track-changes'
 $doc.Undo(1) | Out-Null
 $doc.Hyperlinks.Item(1).Range.Select();$api.SetTracking($false);$api.Generate(2)
 Wait-Proposals
 if($api.State -ne 'idle'){throw 'Varsayılan link koruması düzenlenebilir öneri üretti.'}
 $api.SetLinks($false);$doc.Hyperlinks.Item(1).Range.Select();$api.Generate(2)
 Wait-Proposals
 if($api.State -ne 'ready' -or $api.Apply() -ne 'applied'){throw 'Hyperlink görünen metni değiştirilemedi.'}
 if($doc.Hyperlinks.Count -ne 1 -or $doc.Hyperlinks.Item(1).Address -ne $linkUrl -or $doc.Hyperlinks.Item(1).Range.Text -eq 'tespit edilmiştir'){throw 'Görünen link metni/URL kontrolü başarısız.'}
 $doc.Undo(1) | Out-Null
 $report.checks+=@('link-protection','editable-link-visible-text-preserves-url')
 $report.passed=$true
}catch{
 $report.error=$_.Exception.Message
 throw
}finally{
 try{if($api){$api.SetTracking($originalTracking);$api.SetLinks($originalLinks);$api.SetStrength($originalStrength)}}catch{}
 try{if($doc){$doc.Close(0)}}finally{
  try{$word.Quit()}finally{
   [Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
   $report | ConvertTo-Json -Depth 5 | Set-Content -Path $ReportPath -Encoding UTF8
  }
 }
}

if($report.passed){Write-Host 'PASS: Word yükleme, atıf/terim/biçim/bağlantı koruma, tek Undo ve Track Changes.'}
