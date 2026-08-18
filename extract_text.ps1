[xml]$doc = Get-Content 'docs/temp_docx/word/document.xml'
$ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
$ns.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main")
$nodes = $doc.SelectNodes('//w:t', $ns)
$nodes | ForEach-Object { $_.InnerText }