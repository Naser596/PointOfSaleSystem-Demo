# Udhëzues Përdorimi

Ky dokument shpjegon faqet kryesore të sistemit dhe si përdoren nga kompania gjatë punës së përditshme.

## POS Ditor

1. Hap faqen `/POS`.
2. Shto produktet në shportë.
3. Zgjidh klientin kur shitja duhet të lidhet me një klient.
4. Përfundo shitjen.
5. Nëse sistemi është offline, shitja ruhet në queue lokale dhe mund të sinkronizohet më vonë nga `/OfflineSync`.

## Dokumentet e Shitjes

1. Hap faqen `/SalesDocuments`.
2. Krijo ofertë, porosi ose faturë.
3. Shto produktet/shërbimet në dokument.
4. Përdor rrjedhën normale të dokumenteve: `Quote -> Order -> Invoice`.
5. Regjistro pagesat e faturës nga faqja e detajeve të faturës.
6. Për korrigjime përdor `Credit Note`, jo fshirje direkte të dokumentit.

## Blerjet dhe Furnizimi

1. Hap faqen `/Purchasing`.
2. Krijo purchase order për mallrat që do të blihen.
3. Shto linjat e blerjes me produktet, sasitë dhe çmimet.
4. Kur malli arrin, regjistro pranimin e tij në magazinë.
5. Krijo faturën e furnitorit dhe lidhe me purchase order ose me pranimin e mallit.

## Financat

1. Hap `/FinancialAccounts` për të krijuar llogari cash ose bankare.
2. Shëno transaksionet bankare si të pajtuara kur përputhen me aktivitetin real të bankës.
3. Hap `/Obligations` për paga, qira, taksa, borxhe, avance dhe pagesa të papaguara të kompanisë.
4. Përdor `Pay & Post` për të shënuar obligimin si të paguar dhe për të krijuar regjistrimin financiar.

## Accounting

1. Hap faqen `/Accounting`.
2. Menaxho planin kontabël dhe periudhat fiskale.
3. Kontrollo `Trial Balance`, `Profit & Loss`, `Balance Sheet` dhe journal entries.
4. Kjo pjesë përdoret për të parë nëse financat janë të balancuara dhe për të kuptuar fitimin, humbjen, asetet dhe detyrimet.

## Raportet

1. Hap faqen `/Reports`.
2. Zgjidh date range.
3. Kontrollo fitimin, cashflow, borxhet e klientëve, detyrimet ndaj furnitorëve, vlerën e stokut, ndryshimet në blerje dhe customer statements.
4. Eksporto në Excel kur raporti duhet ruajtur, dërguar ose analizuar më tej.

## HR dhe Payroll

1. Hap faqen `/Hr`.
2. Shto punëtorët.
3. Krijo payroll run me bonus, zbritje dhe përqindje takse.
4. Kontrollo obligimin e pagës te `/Obligations`.
5. Kur paga paguhet, obligimi duhet të shënohet si i paguar që të lidhet me financat.

## Approvals

1. Hap faqen `/Approvals`.
2. Krijo rregulla aprovimi për veprime të ndjeshme, si blerje me vlerë të lartë ose ndryshime të rëndësishme.
3. Kontrollo kërkesat pending.
4. Aprovo ose refuzo kërkesat sipas përgjegjësisë së menaxherit.

## Audit Logs

1. Hap faqen `/AuditLogs`.
2. Filtro sipas action, entity ose datës.
3. Kontrollo kush e bëri ndryshimin, kur u bë dhe çfarë u ndryshua.
4. Eksporto Excel për kontroll të brendshëm, auditim ose dokumentim.
