# Deployment Guide — Windows Server / IIS / PostgreSQL

คู่มือนี้ใช้สำหรับ deploy ระบบ Coach Training & Athlete Attendance Management System ขึ้น Windows Server จริง โดยใช้ค่า config จาก `appsettings.Production.json`

Backend: ASP.NET Core (.NET 10) — `Backend/CoachTraining.Api`
Frontend: Angular 21 — `Frontend`
Database: PostgreSQL

---

## 1. Prerequisites (ติดตั้งบน Server)

- Windows Server (2019/2022) พร้อมสิทธิ์ Administrator
- **IIS** พร้อม role/feature ต่อไปนี้ (Server Manager → Add Roles and Features):
  - Web Server (IIS) → Web Server → Application Development → WebSocket Protocol (ถ้าต้องใช้ในอนาคต)
  - Common HTTP Features, Static Content, Default Document
- **.NET 10 Hosting Bundle** (ASP.NET Core Runtime + IIS Module) — ติดตั้งจาก https://dotnet.microsoft.com/download/dotnet/10.0 (เลือก "Hosting Bundle") แล้ว restart server หรือรัน `net stop was /y && net start w3svc`
- **PostgreSQL Server** (เวอร์ชันที่ตรงกับที่ทีมใช้งาน) ติดตั้งและรันเป็น service อยู่แล้ว (ตาม `skill.md` ระบุว่า PostgreSQL environment มีอยู่แล้ว ไม่ต้องสร้าง Docker infra)
- **URL Rewrite Module** สำหรับ IIS (ใช้กับ Angular SPA routing) — https://www.iis.net/downloads/microsoft/url-rewrite
- Node.js LTS (เฉพาะเครื่อง build — ใช้ build frontend ก่อน publish ไม่จำเป็นต้องอยู่บน production server ถ้า build จากเครื่องอื่นแล้วคัดลอกไฟล์ไป)

---

## 2. เตรียมฐานข้อมูล PostgreSQL

1. ใช้ superuser (เช่น `postgres`) สร้าง **database ตัวเปล่า** และ user สำหรับระบบก่อนเริ่มแอปครั้งแรกเสมอ (ถ้ายังไม่มี):
   ```sql
   CREATE USER coachtraining_app WITH PASSWORD 'ตั้งรหัสผ่านที่ปลอดภัย';
   CREATE DATABASE coachtraining OWNER coachtraining_app;
   GRANT ALL ON SCHEMA public TO coachtraining_app;
   ```
   > **สำคัญ:** ห้าม grant สิทธิ์ `CREATEDB` ให้ `coachtraining_app` (ตามหลัก least-privilege) — แอปทำหน้าที่ apply เฉพาะ **migration** (สร้างตาราง) ตอน start เท่านั้น ไม่ได้สร้างตัว database เอง ถ้า database ยังไม่มีอยู่จริงตอนแอป start ครั้งแรก จะเกิด error `permission denied to create database` แล้วแอป crash — ดังนั้นต้องรันขั้นตอนนี้ให้เสร็จ **ก่อน** deploy/start backend เสมอ
2. เตรียม connection string รูปแบบ:
   ```
   Host=<db-server-host>;Port=5432;Database=coachtraining;Username=coachtraining_app;Password=<password>
   ```

> **หมายเหตุ:** ห้ามสร้าง PostgreSQL ผ่าน Docker บน production ตามข้อกำหนดใน `skill.md` — ใช้ PostgreSQL environment ที่มีอยู่แล้วเท่านั้น

---

## 3. ตั้งค่า `appsettings.Production.json`

ไฟล์ `Backend/CoachTraining.Api/appsettings.Production.json` เป็นค่า config หลักที่ใช้ตอน deploy (`ASPNETCORE_ENVIRONMENT=Production`) โดยจะ override ค่าใน `appsettings.json` (base)

ค่าที่ **ต้องกรอกให้ครบก่อน deploy จริง**:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "Default": "Host=<db-server-host>;Port=5432;Database=coachtraining;Username=coachtraining_app;Password=<password>"
  },
  "Cors": {
    "AllowedOrigins": ["https://<โดเมน frontend จริง>"]
  },
  "Jwt": {
    "Issuer": "CoachTraining.Api",
    "Audience": "CoachTraining.Client",
    "SigningKey": "<สุ่มค่า secret key ความยาวอย่างน้อย 32 ตัวอักษร>",
    "ExpiryMinutes": 480
  },
  "PasswordReset": {
    "FrontendResetUrl": "https://<โดเมน frontend จริง>/reset-password",
    "TokenExpiryMinutes": 10
  },
  "Smtp": {
    "Host": "<smtp host ถ้ามีการส่งอีเมลรีเซ็ตรหัสผ่าน>",
    "Port": 587,
    "UseSsl": false,
    "Username": "",
    "Password": "",
    "FromEmail": "",
    "FromName": "Coach Training"
  }
}
```

### ข้อควรระวังด้าน Security

- **ห้าม commit ค่า secret จริง** (`ConnectionStrings:Default`, `Jwt:SigningKey`, `Smtp:Password`) ลง git — ให้ตั้งค่าเหล่านี้ผ่านใดวิธีหนึ่งต่อไปนี้แทนการแก้ไฟล์ตรง ๆ บน repo:
  - **Environment Variables** ที่ตั้งใน IIS Application Pool / `web.config` (ดูข้อ 5) เช่น `ConnectionStrings__Default`, `Jwt__SigningKey`
  - หรือใช้ `appsettings.Production.json` ที่อยู่เฉพาะบน server (ไม่ commit กลับเข้า git) โดย copy จาก template แล้วกรอกค่าจริงที่ server เท่านั้น
- `Jwt:SigningKey` ต้องเป็นค่าที่สุ่มและเก็บเป็นความลับ (แนะนำ ≥32 ตัวอักษร แบบสุ่ม)
- `Cors:AllowedOrigins` ต้องระบุ origin ของ frontend จริงเท่านั้น ห้ามปล่อยว่างหรือใช้ `*` บน production

---

## 4. Publish Backend (.NET API)

จากเครื่อง build (หรือ server ถ้าติดตั้ง .NET SDK ไว้):

```bash
cd Backend/CoachTraining.Api
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

ตรวจสอบว่า `dotnet build` **ไม่มี error** ก่อนดำเนินการต่อ (ตามข้อกำหนด build validation ใน `CLAUDE.md`)

### EF Core Migration — รันอัตโนมัติตอน Startup

`Program.cs` ถูกตั้งค่าให้แอป apply pending EF Core migration ให้อัตโนมัติทุกครั้งที่ start (ก่อนขั้นตอน seed roles/default Administrator) โดย:

- ตรวจสอบ pending migrations ด้วย `db.Database.GetPendingMigrationsAsync()`
- ถ้ามี pending migration จะเรียก `db.Database.MigrateAsync()` และ log จำนวน/รายชื่อ migration ที่ apply ผ่าน logger `"DbMigrator"`
- ถ้าไม่มี pending migration จะ log ว่าไม่มีอะไรต้อง apply แล้วข้ามไป
- การทำงานนี้ **idempotent** — รันซ้ำได้ทุกครั้งที่ Application Pool start/recycle โดยไม่มีผลเสีย (ถ้า schema ล่าสุดอยู่แล้วจะไม่มี pending migration ให้ apply)

ดังนั้น **ไม่ต้องรัน `dotnet ef database update` เอง** ในขั้นตอน deploy ปกติ — แค่คัดลอกโฟลเดอร์ `publish` (ซึ่งมีไฟล์ migration ฝังอยู่ใน assembly แล้ว) ไปที่ server แล้ว restart Application Pool ตามข้อ 5

> **ข้อควรระวัง:** เนื่องจากแอป start แล้ว apply migration ทันที ให้ตรวจสอบก่อน deploy เสมอว่า:
> - `Backend/CoachTraining.Api/Migrations` มี migration ล่าสุดครบถ้วนตรงกับโค้ดที่จะ deploy
> - ได้ backup ฐานข้อมูล production ไว้ก่อน deploy ทุกครั้ง (ดูข้อ 9 Rollback Plan) เพราะ migration จะถูก apply ทันทีตอน Application Pool เริ่มทำงาน โดยไม่มีขั้นตอนยืนยันแยกต่างหาก
> - รัน backend เพียง **instance เดียว** ต่อฐานข้อมูลระหว่างช่วง migration (หลีกเลี่ยงหลาย instance apply migration พร้อมกัน)
>
> หากต้องการควบคุมเวลาการรัน migration แยกจากการ start แอป (เช่นต้องการรันเป็นขั้นตอนแยกใน pipeline) สามารถแจ้งให้ปรับโค้ดเพิ่มเป็น config flag (เช่น `ApplyMigrationsOnStartup`) ภายหลังได้

### คัดลอกไฟล์ไป Server

คัดลอกโฟลเดอร์ `publish` ทั้งหมดไปที่ server เช่น:

```
C:\inetpub\coachtraining-api\
```

นำ `appsettings.Production.json` ที่กรอกค่าจริงแล้ว (ตามข้อ 3) วางทับในโฟลเดอร์นี้ (หรือใช้ environment variables แทน — ห้ามใส่ secret ในไฟล์ที่จะ commit กลับ repo)

---

## 5. ตั้งค่า IIS สำหรับ Backend API

1. เปิด **IIS Manager**
2. สร้าง **Application Pool** ใหม่ เช่น `CoachTrainingApiPool`
   - .NET CLR version: **No Managed Code** (ASP.NET Core รันเอง ไม่ผ่าน CLR ของ IIS)
   - Start mode: `AlwaysRunning`
3. สร้าง **Site** ใหม่ (หรือ Application ภายใต้ site ที่มีอยู่)
   - Physical path: `C:\inetpub\coachtraining-api`
   - Application Pool: เลือก `CoachTrainingApiPool`
   - Binding: ตั้ง Host name / Port ตามที่ต้องการ (แนะนำใช้ HTTPS พร้อม SSL certificate)
4. ตั้งค่า **Environment Variables** ของ Application Pool (สำหรับ secret ที่ไม่ต้องการเก็บในไฟล์):
   - IIS Manager → เลือก Site → **Configuration Editor** → section `system.webServer/aspNetCore` → แก้ `environmentVariables` collection เพิ่ม:
     - `ASPNETCORE_ENVIRONMENT` = `Production`
     - `ConnectionStrings__Default` = `<connection string จริง>` (ถ้าไม่ต้องการเก็บในไฟล์ json)
     - `Jwt__SigningKey` = `<secret key จริง>`
   - หรือแก้ไฟล์ `web.config` ที่ถูกสร้างจาก `dotnet publish` ให้มี `<environmentVariables>` ตามตัวอย่าง:
     ```xml
     <aspNetCore processPath="dotnet" arguments=".\CoachTraining.Api.dll"
                 stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout"
                 hostingModel="inprocess">
       <environmentVariables>
         <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
       </environmentVariables>
     </aspNetCore>
     ```
5. ตรวจสอบสิทธิ์ folder: ให้ IIS_IUSRS หรือ Application Pool identity มีสิทธิ์ Read & Execute บนโฟลเดอร์ publish
6. Restart Application Pool แล้วทดสอบเรียก API endpoint (เช่น health check หรือ swagger ถ้าเปิดใน production)

---

## 6. Build & Deploy Frontend (Angular)

จากเครื่อง build:

```bash
cd Frontend
npm ci
ng build --configuration production
```

ตรวจสอบว่า `ng build` **สำเร็จโดยไม่มี error** ก่อนดำเนินการต่อ

ไฟล์ output จะอยู่ที่ `Frontend/dist/<project-name>/browser` (ตรวจสอบ path จริงจาก `angular.json` → `outputPath` เนื่องจาก Angular 21 ใช้ builder แบบ application ซึ่งอาจสร้าง sub-folder `browser`)

ก่อน build ให้ตรวจสอบว่าไฟล์ environment ของ frontend (เช่น `src/environments/environment.production.ts` หรือเทียบเท่า) ชี้ **base URL ของ API production** ที่ถูกต้อง (ตรงกับ binding ที่ตั้งใน IIS ข้อ 5)

### ตั้งค่า IIS สำหรับ Frontend (Static Site)

1. คัดลอกไฟล์ทั้งหมดในโฟลเดอร์ build ไปที่ เช่น `C:\inetpub\coachtraining-web\`
2. สร้าง **Site** ใหม่ใน IIS
   - Physical path: `C:\inetpub\coachtraining-web`
   - Binding: ตั้ง Host name / Port (แนะนำ HTTPS)
3. เพิ่มไฟล์ `web.config` ในโฟลเดอร์นี้ เพื่อรองรับ Angular client-side routing (ต้องมี **URL Rewrite Module** ติดตั้งแล้ว):
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <system.webServer>
       <rewrite>
         <rules>
           <rule name="Angular Routes" stopProcessing="true">
             <match url=".*" />
             <conditions logicalGrouping="MatchAll">
               <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
               <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
             </conditions>
             <action type="Rewrite" url="/index.html" />
           </rule>
         </rules>
       </rewrite>
     </system.webServer>
   </configuration>
   ```
   > **หมายเหตุ:** ไม่ต้องเพิ่ม `<mimeMap fileExtension=".json" .../>` เอง — Windows Server ส่วนใหญ่ (2016 ขึ้นไป) ลงทะเบียน MIME type `.json` ไว้ที่ `applicationHost.config` ระดับ server อยู่แล้ว การเพิ่มซ้ำจะทำให้ IIS parse `web.config` ไม่ผ่าน (`500.19 — Cannot add duplicate collection entry`) และล่มทั้งไซต์ (รวมถึง static file ทุกไฟล์ เช่น favicon.ico) ถ้าจำเป็นต้องระบุเอง ให้ `<remove>` ก่อนเสมอ:
   > ```xml
   > <staticContent>
   >   <remove fileExtension=".json" />
   >   <mimeMap fileExtension=".json" mimeType="application/json" />
   > </staticContent>
   > ```
4. ตรวจสอบสิทธิ์ folder: ให้ IIS_IUSRS มีสิทธิ์ Read บนโฟลเดอร์นี้

---

## 7. CORS Configuration Check

หลัง deploy ทั้งสองฝั่งแล้ว ตรวจสอบว่า `Cors:AllowedOrigins` ใน `appsettings.Production.json` (backend) ตรงกับโดเมน/พอร์ตของ frontend site จริง (ข้อ 6) มิฉะนั้น frontend จะเรียก API ไม่ได้ (CORS error)

---

## 8. Post-Deployment Verification Checklist

- [ ] `dotnet build` ผ่านสำเร็จก่อน publish backend
- [ ] `ng build --configuration production` ผ่านสำเร็จก่อน deploy frontend
- [ ] Database ว่าง (`CREATE DATABASE ... OWNER coachtraining_app`) ถูกสร้างไว้แล้วบน PostgreSQL server **ก่อน** start backend ครั้งแรก (ดูข้อ 2) — มิฉะนั้นแอปจะ crash ด้วย `permission denied to create database`
- [ ] มี migration ล่าสุดครบถ้วนใน `Migrations` folder ตรงกับโค้ดที่ deploy (แอปจะ apply ให้อัตโนมัติตอน start — ดูข้อ 4)
- [ ] Backup ฐานข้อมูล production ไว้ก่อน restart Application Pool ครั้งแรกหลัง deploy (เพราะ migration จะรันทันทีตอน start)
- [ ] `appsettings.Production.json` บน server มีค่า `ConnectionStrings:Default`, `Jwt:SigningKey`, `Cors:AllowedOrigins` ครบและถูกต้อง (และไม่ได้ commit ค่า secret จริงกลับเข้า git)
- [ ] Application Pool ของ backend รันด้วย .NET CLR = No Managed Code, ตั้ง `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Backend site ตอบสนอง (ทดสอบ login หรือ endpoint พื้นฐาน)
- [ ] Frontend site โหลดได้ และ routing (refresh หน้าใน sub-route) ทำงานถูกต้องผ่าน `web.config` rewrite rule
- [ ] Frontend เรียก backend API ได้โดยไม่มี CORS error
- [ ] HTTPS/SSL certificate ติดตั้งถูกต้องทั้งสอง site (ถ้าใช้งานจริงบน production ควรบังคับ HTTPS)
- [ ] ทดสอบ business scenario หลัก (login, ดู session, บันทึก attendance) บน environment จริงอย่างน้อย 1 รอบ

---

## 9. Rollback Plan

- เก็บ backup ของ `publish` folder เวอร์ชันก่อนหน้าไว้เสมอก่อน deploy ทับ
- เก็บ backup ฐานข้อมูล PostgreSQL ก่อนรัน migration ใหม่ทุกครั้ง (`pg_dump`)
- หากเกิดปัญหาหลัง deploy ให้ swap โฟลเดอร์ publish กลับเป็นเวอร์ชันก่อนหน้า และ restart Application Pool
