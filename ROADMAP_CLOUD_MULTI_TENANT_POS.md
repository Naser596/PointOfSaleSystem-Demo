# Roadmap per Cloud POS Multi-Company

Data: 2026-04-30

Ky dokument shton planin profesional per ta kthyer POS-in aktual ne platforme cloud, multi-company/multi-tenant, me Super Admin, PostgreSQL, Docker, offline mode, taksa, fitim, kthime produktesh dhe UI/raporte me serioze.

## 1. Vizioni i ri i platformes

Sistemi duhet te funksionoje si platforme cloud ku nje Super Admin menaxhon kompani te ndryshme. Cdo kompani ka adminin/menaxherin e vet, userat e vet, produktet e veta, shitjet e veta, raportet e veta dhe konfigurimet e veta. Te dhenat e nje kompanie nuk duhet te perzihen kurre me te dhenat e kompanise tjeter.

Hierarkia e aksesit:

- `SuperAdmin`: pronari i platformes. Krijon kompani, admin per kompani, sheh qarkullim global dhe monitoron platformen.
- `CompanyAdmin`: pronari/menaxheri i nje kompanie. Menaxhon userat, produktet, shitjet, raportet dhe konfigurimet e kompanise se tij.
- `Manager`: opsional. Menaxhon operacionet ditore, stafin, raportet dhe inventarin.
- `Cashier`: perdor POS-in, ben shitje, kthime sipas lejeve, dhe sheh raportet e veta.

Objektivi kryesor: izolim total i te dhenave per kompani dhe kontroll qendror nga Super Admin.

## 2. Multi-tenant model

Per te mos u ngaterruar te dhenat e kompanive, platforma duhet te kete `CompanyId` ne cdo tabele biznesi.

Entitete kryesore te reja:

- `Company`
- `CompanyUser`
- `Store`
- `Register`
- `RegisterSession`
- `SubscriptionPlan` ose `BillingPlan`
- `CompanySettings`
- `TaxSettings`

Tabelat ekzistuese qe duhet te lidhen me kompani:

- `Products`
- `Categories`
- `Sales`
- `SaleItems`
- `Customers`
- `Discounts`
- `StockMovements`
- `Returns`
- `Payments`
- `Reports`
- `AuditLogs`

Rregulli teknik:

- Cdo query duhet te filtrohet me `CompanyId`.
- Useri normal nuk duhet te dergoje `CompanyId` nga UI si vlere e besuar.
- `CompanyId` duhet te merret nga user claims/session pas login.
- SuperAdmin mund te zgjedhe kompanine nga paneli, por CompanyAdmin/Cashier shohin vetem kompanine e tyre.

Rekomandim: perdor EF Core global query filters per `CompanyId`, por mos u mbeshtet vetem aty. Kontrollo edhe ne service layer.

## 3. Super Admin panel

Super Admin duhet te kete faqe te vecante, jo te perzihet me dashboard-in e kompanise.

Funksionet e Super Admin:

- krijon kompani te re;
- krijon adminin e kompanise;
- vendos/ndryshon emrin publik te kompanise;
- ngarkon/ndryshon logon e kompanise;
- konfigurimin e brandimit per kompani, p.sh. ngjyra kryesore, receipt header dhe emri qe shfaqet ne dashboard;
- aktivizon/caktivizon kompani;
- sheh qarkullimin total per platformen;
- sheh qarkullimin per kompani;
- sheh numrin e userave, shitjeve, produkteve dhe dyqaneve per kompani;
- menaxhon planet/subscriptions nese platforma shitet si SaaS;
- sheh error logs dhe audit logs;
- ben impersonation te kontrolluar, nese vendoset te lejohet;
- reseton password per adminet e kompanive;
- menaxhon konfigurime globale.

Dashboard i Super Admin:

- total companies;
- active companies;
- monthly platform revenue;
- total transactions;
- top companies by revenue;
- companies with low activity;
- failed jobs/errors;
- storage/database usage;
- subscription status.

## 4. Company branding / white-label

Platforma eshte menduar per kompani te ndryshme, prandaj cdo kompani duhet te kete ndjesine e ambientit te vet. SuperAdmin duhet te kete mundesi te personalizoje te dhenat vizuale dhe publike te kompanise.

Funksionet ne SuperAdmin:

- ndryshim i emrit te kompanise;
- upload logo per kompanine;
- zgjedhje ngjyre kryesore per UI;
- zgjedhje ngjyre sekondare ose accent;
- konfigurim i emrit qe shfaqet ne dashboard;
- konfigurim i header-it ne receipt/fature;
- konfigurim i adreses, telefonit, emailit dhe tax number;
- preview si do duket dashboard-i dhe receipt-i per ate kompani;
- aktivizim/caktivizim i brandimit personal.

Fusha te rekomanduara ne `Company`:

```text
Id
LegalName
DisplayName
LogoUrl
PrimaryColor
AccentColor
Address
Phone
Email
TaxNumber
IsActive
CreatedAt
UpdatedAt
```

Rregulla:

- `LegalName` perdoret per kontrata, faturim dhe administrim serioz.
- `DisplayName` shfaqet ne UI per klientin dhe userat e kompanise.
- `LogoUrl` ruan path/URL te logos.
- Logo nuk duhet ruajtur direkt ne database si binary per versionin e pare; me mire ruhet ne file storage dhe database ruan URL/path.
- SuperAdmin mund ta ndryshoje brandimin e cdo kompanie.
- CompanyAdmin mund te lejohet ta ndryshoje logon/emrin publik vetem nese platforma e lejon me permission.

Ku duhet te shfaqet logo/emri:

- login screen, nese useri hyn nga subdomain/link i kompanise;
- sidebar/header pas login;
- dashboard i CompanyAdmin;
- POS screen per cashier;
- receipt/fatura;
- raportet Excel/PDF;
- email notifications ne faze te ardhshme.

Opsione per URL:

```text
platforma.com/login
platforma.com/company/company-slug/login
company-slug.platforma.com
```

Rekomandimi praktik per fillim:

- perdor `platforma.com/company/{slug}` ose zgjedhje kompanie pas login;
- subdomain per cdo kompani mund te shtohet me vone;
- brandimi ngarkohet pasi sistemi e di `CompanyId` te userit.

UI ne SuperAdmin:

- faqe `Companies`;
- buton `Create Company`;
- faqe `Company Details`;
- tab `Branding`;
- fusha `Display Name`, `Logo`, `Primary Color`, `Accent Color`;
- preview panel ne te djathte;
- buton `Save Branding`.

Kjo krijon ndjesi te personalizuar per klientin pa krijuar aplikacion te ndare per cdo kompani.

## 5. Company Admin panel

Company Admin duhet te shohi vetem kompanine e vet.

Funksionet:

- menaxhim userash te kompanise;
- menaxhim role per staff;
- produkte, kategori, barcode, cmime;
- inventar dhe low stock;
- raporte ditore/javore/mujore;
- kthime produktesh;
- taksa dhe konfigurime financiare;
- pagesat e puntoreve;
- fitimi mujor;
- konfigurim i receipt/fatures;
- konfigurim i dyqaneve/registerave nese kompania ka me shume se nje lokacion.

## 6. Low stock dhe disable produkti

Kjo pjese ekziston pjeserisht ne dashboard: produktet me low stock dalin per alert. Duhet te zgjerohet ne workflow me aksione.

Rekomandim:

- ne dashboard te shfaqen produktet low stock me butona `Restock`, `Disable`, `View History`;
- `Restock` hap modal/page ku admini shton sasine, furnitorin, shenim dhe cmimin bleres nese ka ndryshuar;
- `Disable` e ben produktin jo aktiv, pa e fshire;
- cdo restock/disable duhet te krijoje `AuditLog`;
- cashier nuk duhet te beje restock/disable pa permission;
- produkti disabled nuk duhet te shfaqet ne POS.

## 7. Raportet ditore, javore dhe mujore

Ti ke implementuar qe cdo account te gjeneroje raport brenda dites dhe javes. Per platforme serioze duhet te standardizohet raportimi sipas rolit.

Per Cashier:

- shitjet e veta sot;
- shitjet e veta kete jave;
- payment method breakdown;
- kthimet/refunds qe ka bere;
- total cash/card.

Per CompanyAdmin:

- shitjet e gjithe kompanise;
- raport per cashier;
- raport per store/register;
- raport per produkt/kategori;
- raport zbritjesh;
- raport kthimesh;
- raport fitimi;
- raport taksash;
- raport pagash;
- export Excel/PDF.

Per SuperAdmin:

- qarkullimi per cdo kompani;
- numri i transaksioneve per kompani;
- aktiviteti mujor;
- statusi i abonimit;
- krahasime ndermjet kompanive pa ekspozuar te dhena sensitive te klientit final, nese nuk eshte e nevojshme.

### 7.1 Dashboard ditor per cashier dhe historik per admin

Opsioni qe ke vendosur per user/cashier eshte i mire: dashboard-i i cashier mund te jete i fokusuar vetem ne diten aktuale. Ne fund te dites gjenerohet raporti ditor dhe ekrani i cashier rifreskohet per diten e re.

Rregulli i rendesishem: te dhenat nuk duhet te fshihen. Duhet te rifreskohet vetem pamja ditore e cashier, ndersa shitjet dhe raportet ruhen ne database si historik.

Per Cashier dashboard:

- shfaq vetem performancen e dites aktuale;
- shfaq total shitje sot;
- shfaq total cash/card sot;
- shfaq numrin e transaksioneve sot;
- shfaq kthimet/refunds sot;
- ne fund te dites gjeneron raport ditor;
- diten tjeter dashboard fillon nga zero per pamjen ditore;
- raportet e vjetra nuk duhet te zhduken, vetem nuk jane fokusi kryesor i cashier.

Per CompanyAdmin dashboard:

- nuk duhet te rifreskohet historiku;
- admini duhet te shohe raportet e cdo dite, jave dhe muaji;
- admini duhet te filtroje sipas userit, dates, javes, muajit, store/register dhe payment method;
- admini duhet te shohe progresin e secilit user ne kohe;
- kur klikon nje user, hapet profili/performance page i atij useri.

Faqe e rekomanduar: `User Performance`

Kjo faqe hapet nga admini kur klikon nje cashier/user dhe duhet te kete:

- KPI cards: shitjet sot, shitjet kete jave, shitjet kete muaj, numri i transaksioneve, average basket, refunds;
- chart daily per muajin aktual;
- chart weekly per 8-12 javet e fundit;
- chart monthly per vitin aktual;
- krahasim me periudhen e kaluar;
- lista e shitjeve te fundit;
- lista e kthimeve/refunds;
- export per kete user ne Excel/PDF.

Entitete ose views te rekomanduara:

- `DailyUserReport`
- `WeeklyUserReport`
- `MonthlyUserReport`
- `ReportSnapshot`

Mund te krijohen si tabela te ruajtura ose si database views/materialized views. Per fillim, mjafton te llogariten nga `Sales` me query te optimizuara. Me vone, kur te rritet volumi, krijohen snapshot tables per performace me te mire.

Rregull raportimi:

```text
Cashier dashboard = pamje operative ditore
Admin dashboard = historik i plote dhe performance analytics
SuperAdmin dashboard = analytics per kompani/platforme
```

Kjo ndarje e ben sistemin me te qarte: useri punon shpejt gjate dites, admini analizon progresin dhe kompania mban historik te plote.

### 7.2 Business Sales Dashboard

Per ta bere dashboard-in e shitjeve me serioz dhe me orientim biznesi, duhet te mos shfaqe vetem total shitje. Duhet te tregoje shendetin e biznesit: revenue, profit, trend, performanca e stafit, produktet me fitim, refunds, taxes dhe cash flow.

Dashboard i rekomanduar per CompanyAdmin:

- `Gross Revenue`: totali bruto i shitjeve.
- `Net Revenue`: shitjet pas zbritjeve dhe kthimeve.
- `Gross Profit`: shitje minus cmimi bleres i produkteve.
- `Profit Margin`: perqindja e fitimit.
- `Average Basket Value`: mesatarja e nje transaksioni.
- `Transactions Count`: numri i transaksioneve.
- `Refund Rate`: perqindja e kthimeve.
- `Discount Impact`: sa revenue eshte ulur nga zbritjet.
- `Tax Collected`: taksat e mbledhura.
- `Cash vs Card`: ndarja sipas menyres se pageses.
- `Top Products by Revenue`.
- `Top Products by Profit`.
- `Low Stock Risk`: produkte qe mund te ndalin shitjen.
- `Cashier Performance`: krahasim i userave.

Charts te rekomanduar:

- revenue trend per dite/jave/muaj;
- profit trend per dite/jave/muaj;
- sales by payment method;
- sales by category;
- top products by revenue/profit;
- refunds over time;
- hourly sales heatmap per te pare oret me trafik;
- cashier leaderboard;
- comparison me periudhen e kaluar, p.sh. sot vs dje, kjo jave vs java e kaluar, ky muaj vs muaji i kaluar.

Filtrat qe duhet te kete dashboard-i:

- today;
- yesterday;
- this week;
- last week;
- this month;
- last month;
- custom date range;
- store/register;
- cashier/user;
- category;
- payment method.

KPI cards duhet te kene:

- vleren aktuale;
- ndryshimin ne perqindje krahasuar me periudhen e kaluar;
- indikator pozitiv/negativ;
- tooltip qe shpjegon cfare llogaritet.

Ky dashboard duhet te jete faqja kryesore per adminin e kompanise, sepse i tregon menjehere si po ecen biznesi.

## 8. PostgreSQL migration

SQLite duhet te zevendesohet me PostgreSQL per cloud.

Hapat:

1. Shto package `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. Ndrysho connection string ne environment variables.
3. Krijo database PostgreSQL ne Docker per development.
4. Rishiko migrimet ekzistuese.
5. Krijo migration te re per multi-tenant fields.
6. Testo decimal precision, indexes, foreign keys dhe query filters.
7. Krijo seed data te ndare per SuperAdmin dhe kompani demo.

Connection string shembull:

```text
Host=postgres;Port=5432;Database=pos_platform;Username=pos_user;Password=pos_password
```

Per prodhim, password nuk duhet te ruhet ne repo. Duhet te vije nga secrets/environment.

## 9. Docker dhe task setup

Qellimi eshte te mos varesh nga `dotnet build` manual ne makine lokale. Duhet nje setup i qarte me Docker.

File te rekomanduar:

- `Dockerfile`
- `docker-compose.yml`
- `.dockerignore`
- `Taskfile.yml` ose `Makefile`
- `.env.example`

Sherbime ne Docker Compose:

- `web`: ASP.NET Core app;
- `postgres`: database;
- `pgadmin`: opsional per development;
- `redis`: opsional per cache/offline sync/jobs ne faze te ardhshme.

Taska te rekomanduar:

```text
task setup       # build containers, restore packages, start db
task up          # start app + postgres
task down        # stop containers
task logs        # show app logs
task migrate     # run EF migrations
task seed        # seed SuperAdmin/demo company
task test        # run tests
task reset-db    # recreate dev database
```

Nese nuk perdor Taskfile, mund te perdoret `make`, por ne Windows `Taskfile.yml` eshte zakonisht me praktik.

## 10. Offline mode

Offline mode eshte pjese kritike per POS, sepse interneti mund te bjere. Duhet menduar me kujdes qe mos te krijohen shitje duplicate ose stok i pasakte.

Opsioni A: cloud-only

- me i thjeshte;
- nese nuk ka internet, POS nuk punon;
- i pershtatshem per fazen e pare cloud MVP.

Opsioni B: offline-first per cashier

- browser/app ruan cart dhe shitje lokale;
- kur kthehet interneti, sinkronizon shitjet;
- duhet konflikt resolution per stokun;
- duhet numbering lokal per receipt;
- duhet queue per pending sales;
- me kompleks, por me profesional per dyqane reale.

Rekomandimi praktik:

- Faza 1: cloud-only me mesazh te qarte kur s'ka internet.
- Faza 2: offline queue per shitje cash.
- Faza 3: offline sync me conflict handling per stok, returns dhe discounts.

Rregulla per offline:

- cashier mund te shese vetem produkte qe jane sinkronizuar me pare;
- discount codes offline duhet te jene cached ose te mos lejohen;
- card payment offline nuk duhet te lejohet pa integrim terminali qe e mbeshtet;
- kur sync deshton, shitja mbetet `PendingSync`;
- admin duhet te shoh pending/offline sales.

## 11. Taksa, cmim bleres, cmim shites dhe fitim

Duhet faqe e vecante financiare, p.sh. `Finance` ose `CompanyFinance`.

Fusha te reja te rekomanduara:

Ne `Product`:

- `PurchasePrice` ose `CostPrice`;
- `SellingPrice`;
- `TaxRateId`;
- `TrackInventory`;
- `SupplierId`.

Ne `SaleItem` duhet te ruhen snapshot-et:

- `ProductNameSnapshot`;
- `CostPriceSnapshot`;
- `SellingPriceSnapshot`;
- `TaxRateSnapshot`;
- `TaxAmount`;
- `GrossProfit`.

Pse snapshot?

Sepse cmimi bleres/shites mund te ndryshoje me vone, por raporti historik i shitjes duhet te mbetet i sakte.

Raporti financiar mujor duhet te shfaqe:

- total revenue;
- total cost of goods sold;
- gross profit;
- discounts total;
- tax collected;
- refunds total;
- payroll obligations;
- other expenses;
- net profit estimate.

Formula baze:

```text
Gross Profit = Sales Revenue - Cost of Goods Sold
Net Profit = Gross Profit - Payroll - Other Expenses - Taxes/Obligations
```

Kujdes: taksat duhet te konfigurohen sipas vendit/rregullave reale te biznesit. Sistemi duhet te kete konfigurim fleksibel, jo vlera hardcoded.

## 12. Pagat dhe detyrimet e puntoreve

Per pagesat e puntoreve duhet modul i ndare.

Entitete te rekomanduara:

- `EmployeeProfile`
- `PayrollPeriod`
- `PayrollEntry`
- `PayrollPayment`

Fusha:

- paga mujore ose ore pune;
- bonus/komision;
- zbritje/detyrime;
- periudha;
- status: draft, approved, paid;
- approved by;
- paid date.

Raporti i kompanise duhet te llogarise:

- detyrimet mujore per paga;
- sa eshte paguar;
- sa ka mbetur;
- ndikimi ne net profit.

## 13. Kthimi i produktit / refund

Aktualisht ke detajet e transaksionit, por duhet search bar dhe workflow per kthim.

Faqe e rekomanduar: `Returns`.

Rrjedha:

1. Cashier/Admin shkruan `SaleNumber`, `ReceiptNumber`, ose `TransactionId`.
2. Sistemi shfaq shitjen dhe produktet e saj.
3. Useri zgjedh produktin dhe sasine per kthim.
4. Sistemi kontrollon nese produkti eshte kthyer me pare.
5. Sistemi krijon `Return` dhe `ReturnItem`.
6. Sistemi rrit stokun, nese produkti kthehet ne gjendje te shitshme.
7. Sistemi krijon `StockMovement` me type `Return`.
8. Sistemi krijon refund/payment adjustment.
9. Sistemi perditeson totalet e raportimit.

Entitete:

- `Return`
- `ReturnItem`
- `RefundPayment`

Statuset e kthimit:

- `Pending`
- `Approved`
- `Rejected`
- `Completed`

Rregulla:

- nuk lejohet kthim me shume se sasia e blere;
- nuk lejohet kthim dy here per te njejten sasi;
- duhet arsye kthimi;
- duhet role/permission;
- cdo kthim duhet audit log.

Search bar duhet te kerkoje sipas:

- SaleNumber;
- ReceiptNumber;
- Customer phone/name;
- barcode produkti brenda shitjes;
- date range.

## 14. Settings dhe faturat e blerjes nga furnizuesit

Platforma duhet te kete nje faqe `Settings` ku te dhenat e kompanise ruhen nje here dhe perdoren automatikisht ne fatura, raporte, receipt dhe dokumente zyrtare. Kjo e ben sistemin me profesional sepse admini nuk ka nevoje te shkruaje logon/emrin/adresen ne cdo dokument.

Te dhenat e kompanise sone qe ruhen nje here:

- emri publik i kompanise;
- emri ligjor;
- logo;
- adresa;
- qyteti/shteti;
- telefoni;
- email;
- tax number / VAT number;
- prefix per fatura;
- tekst standard ne fund te fatures;
- ngjyra kryesore e brandimit ne faze te ardhshme.

Rregulla:

- keto te dhena ruhen ne `Settings`;
- ne fatura ngarkohen automatikisht;
- ka buton `Change`/`Edit Settings` vetem per admin;
- logo ruhet si file dhe database ruan path/URL;
- per multi-company me vone keto fusha kalojne te `Company` ose `CompanySettings` me `CompanyId`.

Faturat e blerjes nga furnizuesi:

- admini krijon fature blerjeje per produktet qe kompania blen nga furnizuesit;
- te dhenat e kompanise sone mbushen automatikisht nga settings;
- te dhenat e furnizuesit plotesohen ne cdo fature, sepse furnizuesi mund te ndryshoje;
- produktet/sherbimet vendosen manualisht ne rreshta;
- sistemi llogarit subtotal, tax dhe total;
- fatura mund te ruhet, shikohet dhe me vone te eksportohet PDF/print;
- ne faze te ardhshme fatura e blerjes mund te lidhet me restock dhe cmimin bleres te produktit.

Te dhenat e furnizuesit per cdo fature:

- emri i furnizuesit;
- tax number / VAT number;
- adresa;
- telefoni;
- email;
- numri i fatures se furnizuesit;
- data e fatures;
- due date;
- shenime.

Rreshtat e fatures:

- pershkrimi;
- sasia;
- cmimi njesi;
- tax rate;
- tax amount;
- line total.

Faza e pare:

- `Settings` per te dhenat e kompanise;
- `Supplier Invoices` per krijim/listim/detaje;
- kalkulim automatik subtotal/tax/total;
- design i paster ne faqe;
- fushat e kompanise ngarkohen automatikisht.

Faza e dyte:

- eksport PDF;
- print layout;
- lidhje me produkte ekzistuese;
- buton `Create Restock From Invoice`;
- supplier directory;
- payment status per faturen e furnizuesit.

## 15. Charts dhe raportim profesional

Charts duhet te jene me te qarta dhe te dobishme per vendimmarrje, jo vetem dekorim.

Rekomandim UI:

- revenue line chart per dite/jave/muaj;
- bar chart per top products;
- pie/donut chart per payment methods;
- stacked chart per sales vs refunds;
- low stock table me aksione;
- profit chart: revenue, cost, gross profit;
- cashier performance chart;
- tax collected chart;
- company comparison chart vetem per SuperAdmin.

Library e rekomanduar:

- Chart.js per thjeshtesi;
- ApexCharts nese do dashboard me pamje me moderne;
- ECharts nese do raporte me te avancuara.

## 16. UI/UX profesional

Duhet ta ndajme UI ne tre zona:

- Super Admin Platform UI;
- Company Admin Backoffice UI;
- Cashier POS UI.

Cashier POS duhet te jete i shpejte:

- search/barcode ne fokus;
- cart i qarte;
- butona te medhenj per pagesa;
- low stock warning pa e bllokuar shpejtesine;
- receipt/print i lehte;
- offline status indicator.

Admin UI duhet te jete e dendur dhe profesionale:

- sidebar;
- tabela me filter/sort/pagination;
- cards te vogla per KPI;
- raporte me date range;
- actions ne row;
- modals vetem kur jane praktike.

SuperAdmin UI duhet te jete e ndare:

- companies list;
- company detail;
- platform analytics;
- subscriptions;
- audit/errors.

Standardi vizual per gjithe aplikacionin:

- layout me sidebar profesional dhe top bar te qarte;
- navigim i ndare sipas rolit, pa menu te panevojshme per userin;
- ngjyra neutrale, me accent nga brandimi i kompanise;
- typography e qarte dhe konsistente;
- KPI cards kompakte, jo te medha pa arsye;
- tabela profesionale me search, filter, sort, pagination dhe bulk actions kur duhen;
- charts me labels, tooltips, date range dhe krahasim me periudhen e kaluar;
- states te qarta per empty/loading/error;
- butona me hierarchy te qarte: primary, secondary, danger;
- modals vetem per aksione te shpejta; format e gjata duhet te jene faqe me vete;
- responsive design per tablet/laptop, por POS-i duhet optimizuar sidomos per ekran kasieri.

Faqet qe duhet te modernizohen me prioritet:

- SuperAdmin Dashboard;
- Companies list/detail/branding;
- CompanyAdmin Dashboard;
- User Performance page;
- POS screen;
- Products/Inventory;
- Sales/Reports;
- Returns/Refunds;
- Finance dashboard.

Qellimi i UI nuk eshte vetem te duket bukur. Duhet te duket serioz, i besueshem dhe te ndihmoje punen ditore pa ngaterruar cashier/admin me elemente te panevojshme.

## 17. Audit log dhe permissions

Per platforme serioze duhet sistem permissions, jo vetem role te thjeshta.

Shembuj permissions:

- `Products.Create`
- `Products.Edit`
- `Products.Disable`
- `Inventory.Restock`
- `Inventory.Adjust`
- `Sales.Create`
- `Sales.Void`
- `Returns.Create`
- `Returns.Approve`
- `Reports.ViewCompany`
- `Reports.ViewOwn`
- `Users.Manage`
- `Finance.View`
- `Finance.Manage`

Audit log duhet te ruaje:

- company;
- user;
- action;
- entity type;
- entity id;
- old values/new values per veprime kritike;
- IP/device;
- timestamp.

## 18. Backend profesional

Pervec UI-se, platforma duhet te kete backend te qendrueshem, te monitorueshem dhe te lehte per zgjerim. Kjo eshte ajo qe e dallon nje MVP nga nje platforme serioze.

### 18.1 API contracts dhe DTO

Controller-at nuk duhet te punojne direkt me entitetet e databazes per veprimet kryesore. Duhet te perdoren request/response DTO.

Shembuj:

- `CreateSaleRequest`
- `CreateSaleResult`
- `ReturnSaleRequest`
- `RestockProductRequest`
- `CreateCompanyRequest`
- `UpdateCompanyBrandingRequest`
- `DashboardFilterRequest`
- `DashboardMetricsResponse`

Kjo e ben backend-in me te paster dhe me te gatshem per API/mobile app ne te ardhmen.

### 18.2 Validation layer

Validimi duhet te jete i centralizuar dhe jo i shperndare ne cdo controller.

Rregulla:

- sasia e shitjes duhet te jete pozitive;
- produkti duhet te jete aktiv;
- stoku duhet te jete i mjaftueshem;
- discount duhet te jete valid;
- useri duhet te kete permission;
- kompania duhet te jete aktive;
- kthimi nuk mund te tejkaloje sasine e blere.

Mund te perdoret FluentValidation ose validation services te brendshme.

### 18.3 Background jobs

Disa pune nuk duhet te varen nga request-i i userit.

Jobs te rekomanduar:

- gjenerim raporti ditor ne fund te dites;
- gjenerim weekly/monthly snapshots;
- kontroll low stock dhe njoftime;
- pastrim temporary files;
- sync offline sales;
- export reports ne background;
- subscription checks.

Teknologji e rekomanduar:

- Hangfire per fillim, sepse eshte praktik ne .NET;
- ose Quartz.NET nese do scheduling me te kontrolluar.

### 18.4 Caching

Per dashboard dhe charts, disa query mund te behen te renda.

Caching i rekomanduar:

- cache per dashboard metrics per 1-5 minuta;
- cache per category/product lists;
- cache per company branding;
- cache per permissions te userit;
- Redis kur platforma shkon ne cloud.

Kujdes: shitja dhe inventari nuk duhet te bazohen vetem ne cache. Ato duhet te verifikohen ne database.

### 18.5 Observability dhe logs

Platforma duhet te kete logs serioze.

Duhet te regjistrohen:

- errors;
- failed login attempts;
- sale failures;
- payment/refund failures;
- stock adjustments;
- permission denials;
- slow queries;
- background job failures.

Rekomandime:

- structured logging me Serilog;
- correlation id per cdo request;
- health checks per app dhe database;
- dashboard per background jobs;
- error tracking ne prodhim.

### 18.6 Error handling profesional

Useri nuk duhet te shohi exception teknike.

Duhet:

- global exception handler;
- error pages te pastra;
- API error response standard;
- mesazhe biznesi te kuptueshme;
- logging i plote ne backend.

Shembull:

```text
Nuk ka stok te mjaftueshem per kete produkt.
```

jo:

```text
DbUpdateException / NullReferenceException
```

### 18.7 Test strategy

Per platforme serioze duhen teste automatike.

Prioritet:

- unit tests per discount, tax, profit, returns;
- integration tests per sale transaction;
- tests per multi-company data isolation;
- tests per permissions;
- tests per dashboard metrics;
- tests per offline sync ne faze te ardhshme.

### 18.8 Performance indexes

Me PostgreSQL duhet te shtohen indekse per query-t kryesore.

Indekse te rekomanduara:

- `Sales(CompanyId, SaleDate)`
- `Sales(CompanyId, CashierId, SaleDate)`
- `SaleItems(CompanyId, ProductId)`
- `Products(CompanyId, Barcode)`
- `Products(CompanyId, SKU)`
- `StockMovements(CompanyId, ProductId, CreatedDate)`
- `Returns(CompanyId, CreatedDate)`
- `AuditLogs(CompanyId, CreatedAt)`

Pa indekse, dashboard-i dhe raportet do behen te ngadalta kur te rriten te dhenat.

## 19. Rekomandime shtese profesionale

### 19.1 Subscription/Billing

Nese platforma do shitet te kompani te ndryshme, duhet menduar billing:

- plan free/trial;
- plan monthly;
- kufi per user/store/register;
- status active/suspended;
- invoice per kompanine.

### 19.2 Notifications

Shto njoftime:

- low stock;
- pending offline sync;
- failed payments;
- subscription expiring;
- high refunds;
- end of day close not completed.

### 19.3 End-of-day close

Per POS real duhet close shift/day:

- cashier hap arken;
- regjistron cash start;
- ben shitje;
- mbyll arken;
- deklaron cash counted;
- sistemi llogarit diferencen.

### 19.4 Import/export

Kompanite do kerkojne:

- import produkte nga Excel;
- export shitje;
- export inventory;
- export reports per accountant.

### 19.5 API-first gradual

Nuk ka nevoje te hidhet MVC menjehere, por logjika duhet te behet API-ready:

- services te pastra;
- DTO;
- validation;
- tests;
- controller-at te jene te holle.

## 20. Roadmap i rekomanduar

### Faza 1 - Themelet cloud

1. Pastro repo dhe rregullo solution.
2. Shto Dockerfile, docker-compose, .dockerignore, .env.example.
3. Migro ne PostgreSQL.
4. Shto SuperAdmin role.
5. Shto `Company`.
6. Lidh userat me kompani.
7. Shto `CompanyId` ne tabelat kryesore.
8. Vendos query filters dhe service-level validation.

### Faza 2 - Admin per kompani

1. SuperAdmin krijon kompani.
2. SuperAdmin krijon CompanyAdmin.
3. CompanyAdmin krijon userat e vet.
4. CompanyAdmin sheh vetem raportet e kompanise se vet.
5. Cashier sheh vetem shitjet e veta.

### Faza 3 - POS serioz

1. Unifiko finalizimin e shitjes ne nje service transaksional.
2. Shto register sessions.
3. Shto end-of-day close.
4. Shto returns/refunds.
5. Shto stock movement te plote.
6. Shto receipt numbering.

### Faza 4 - Finance dhe tax

1. Shto purchase price/cost price.
2. Shto tax settings.
3. Ruaj sale item snapshots.
4. Shto finance dashboard.
5. Shto payroll obligations.
6. Shto monthly profit report.

### Faza 5 - Offline

1. Shto online/offline indicator.
2. Shto pending cart/sale local queue.
3. Shto sync endpoint.
4. Shto conflict handling.
5. Shto admin view per pending sync.

### Faza 6 - UI profesional dhe analytics

1. Rifresko layout me sidebar dhe role-based navigation.
2. Modernizo POS screen.
3. Modernizo dashboard charts.
4. Shto report builder me date range.
5. Shto SuperAdmin analytics.

## 21. Vendimi me i rendesishem teknik

Per multi-company platform, vendimi kryesor eshte izolimi i te dhenave.

Rekomandimi im:

- nje database PostgreSQL e perbashket ne fillim;
- cdo tabele biznesi ka `CompanyId`;
- indexes per `CompanyId`;
- global query filters;
- service-level guard;
- audit log;
- tests qe vertetojne se useri i nje kompanie nuk lexon dot te dhena te kompanise tjeter.

Database e ndare per cdo kompani mund te vije me vone, por per fillim e rrit shume kompleksitetin.

## 22. Cfare duhet bere menjehere

Radha praktike:

1. Krijo Docker/PostgreSQL setup.
2. Shto modelin `Company` dhe lidhjen user-company.
3. Shto SuperAdmin seed nga environment variables.
4. Shto `CompanyId` ne `Product`, `Category`, `Sale`, `Customer`, `Discount`, `StockMovement`.
5. Rregullo query filters.
6. Rregullo POS sale service me transaction.
7. Pastaj nderto SuperAdmin dashboard.

Kjo radhe e mban projektin nen kontroll dhe krijon bazen e duhur per cdo feature tjeter.
