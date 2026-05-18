# Udhëzues për Gatishmëri në Production

Ky dokument është checklist bazë para se platforma të shitet, hostohet ose përdoret nga kompani reale.

## Siguria

- Përdor vetëm HTTPS kur sistemi vendoset online.
- Ruaj kredencialet e databazës në environment variables ose secret manager, jo direkt në kod.
- Roli `SuperAdmin` duhet të jetë vetëm për pronarin/operatorin e platformës.
- Çdo përdorues i kompanisë duhet të marrë rolin më të kufizuar që i duhet: `Cashier`, `Warehouse`, `HR`, `Accountant`, `Manager` ose `Admin`.
- Kontrollo `/AuditLogs` çdo javë dhe eksporto audit logs para ndryshimeve të mëdha ose mirëmbajtjeve që prekin të dhëna.

## Backup-et

- Bëj backup të PostgreSQL çdo ditë.
- Mbaj të paktën 30 backup-e ditore dhe 12 backup-e mujore.
- Testo rikthimin e backup-it një herë në muaj në një databazë tjetër.
- Backup-et duhet të përfshijnë edhe imazhet e kompanive dhe produkteve, jo vetëm databazën.

## Monitorimi

- Monitoro gjendjen e container-ave, diskun, CPU, memorien dhe lidhjet me PostgreSQL.
- Krijo alarme për:
  - shumë login-e të dështuara
  - sync records të dështuara
  - abonime kompanish që kanë skaduar
  - dështime të backup-it të databazës
- Pas çdo deploy kontrollo application logs për gabime runtime.

## Deploy

- Ekzekuto migrimet e databazës para se versioni i ri të përdoret nga klientët.
- Pas deploy bëj smoke-test në këto faqe:
  - `/Reports`
  - `/Accounting`
  - `/POS`
  - `/Purchasing`
  - `/Warehouses`
  - `/OfflineSync`
  - `/SuperAdmin`

## Regjistrimi i Kompanisë

- Krijo kompaninë nga `SuperAdmin` dhe vendos datat e aksesit në platformë.
- Krijo të paktën një përdorues `Admin` për kompaninë.
- Konfiguro brandimin e kompanisë, taksën, valutën, shënimin në fund të faturës dhe shënimin në fund të kuponit.
- Krijo llogaritë financiare, magazinat, përdoruesit, produktet, klientët dhe stokun fillestar.

## Support dhe Mirëmbajtje

- Mbaj shënim konfigurimet specifike për çdo kompani.
- Para se të ndryshosh të dhëna manualisht, eksporto të dhënat që preken dhe shkruaj arsyen.
- Për dokumente të postuara financiarisht përdor kthime, anulime ose credit notes. Mos i fshi direkt dokumentet e biznesit pa arsye të fortë.
