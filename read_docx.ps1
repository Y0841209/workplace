Add-Type -AssemblyName DocumentFormat.OpenXml
$doc = [DocumentFormat.OpenXml.Packaging.WordprocessingDocument]::Open('docs/FRD_Modelo_Datos_Workplace_Booking_OpenCode.docx', $false)
$body = $doc.MainDocumentPart.Document.Body
$body.InnerText