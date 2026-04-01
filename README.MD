# 📦 Inventory Management Solution

<div align="center">

![Version](https://img.shields.io/badge/version-1.0.0-blue?style=for-the-badge)
![Platform](https://img.shields.io/badge/platform-.NET%20Core-purple?style=for-the-badge&logo=dotnet)
![Database](https://img.shields.io/badge/database-MS%20SQL%20Server-red?style=for-the-badge&logo=microsoftsqlserver)
![License](https://img.shields.io/badge/license-All%20Rights%20Reserved-black?style=for-the-badge)

**თანამედროვე, ვებ-ზე დაფუძნებული სასაწყობო მარაგების მართვის სისტემა**

</div>

---

## 📋 პროექტის შესახებ

**Inventory Management Solution** არის სრულფასოვანი ვებ-აპლიკაცია, რომელიც შექმნილია ორგანიზაციებში სასაწყობო მარაგების ეფექტური კონტროლის, აღრიცხვისა და მართვისთვის. სისტემა უზრუნველყოფს პროდუქციის ნაშთების რეალურ დროში მონიტორინგს, მომხმარებელთა წვდომის მართვასა და დეტალურ რეპორტინგს.

---

## ✨ ძირითადი ფუნქციონალი

| ფუნქცია | აღწერა |
|---|---|
| 📦 **პროდუქციის აღრიცხვა** | ნაშთების დამატება, რედაქტირება, კატეგორიზაცია |
| 📊 **მარაგების მონიტორინგი** | ნაშთების კონტროლი რეალურ დროში |
| 👥 **მომხმარებელთა მართვა** | წვდომის კონტროლი და როლების განაწილება |
| 📈 **რეპორტინგი** | ინვენტარიზაციის დეტალური ანგარიშგება |
| 📱 **რესპონსიული დიზაინი** | ოპტიმიზებული ნებისმიერი მოწყობილობისთვის |

---

## 🖥️ UI — ინტერფეისის აღწერა

სისტემის ინტერფეისი შედგება შემდეგი ძირითადი განყოფილებებისაგან:

### მთავარი სამუშაო პანელი (Dashboard)
მთავარი გვერდი აჩვენებს ყველა მნიშვნელოვან მეტრიკას ერთ ხედში — პროდუქციის რაოდენობა, კრიტიკული ნაშთები, ბოლო ოპერაციები და სტატისტიკური გრაფიკები.

### პროდუქციის სია (Products)
ცხრილური ხედი ფილტრაციისა და ძიების შესაძლებლობებით. ყველა ჩანაწერი ექვემდებარება რედაქტირებას, წაშლასა და კატეგორიებში დაჯგუფებას.

### ოპერაციების ჟურნალი (Operations Log)
სრული ისტორია ყველა შემოსვლა-გასვლის ოპერაციისა — ვინ, რა, როდის შეიტანა ან გაიღო.

### მომხმარებლის მართვა (User Management)
ადმინისტრატორის პანელი, სადაც შესაძლებელია მომხმარებლების დამატება, როლების (Admin / Operator / Viewer) მინიჭება და წვდომის კონტროლი.

---

## 🛠️ ტექნოლოგიური სტეკი

```
Backend  ──►  ASP.NET Core (C#)
Frontend ──►  HTML5 · CSS3 · JavaScript (Razor Views)
Database ──►  Microsoft SQL Server
IDE      ──►  Microsoft Visual Studio
```

---

## 📂 პროექტის სტრუქტურა

```
InventoryManagementSolution/
│
├── InventoryManagementWebApp/
│   ├── Controllers/          ← ბიზნეს ლოგიკა
│   ├── Models/               ← მონაცემთა მოდელები
│   ├── Views/                ← UI გვერდები (Razor)
│   └── wwwroot/              ← სტატიკური ფაილები (CSS/JS/Images)
│
├── InventoryManagementSolution.sln
├── .gitignore
└── README.MD
```

---

## ⚙️ გაშვება (Getting Started)

### წინაპირობები

დარწმუნდით, რომ თქვენს სისტემაზე დაინსტალირებულია:

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Microsoft SQL Server](https://www.microsoft.com/sql-server) (LocalDB ან სრული ვერსია)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (ან VS Code)

### ინსტალაცია

```bash
# 1. რეპოზიტორის კლონირება
git clone https://github.com/nikolozkv/InventoryManagementSolution.git

# 2. პროექტის გახსნა
cd InventoryManagementSolution

# 3. კავშირის სტრინგის კონფიგურაცია
# გახსენით appsettings.json და შეავსეთ ConnectionStrings სექცია

# 4. მიგრაციების გაშვება
dotnet ef database update

# 5. პროექტის გაშვება
dotnet run --project InventoryManagementWebApp
```

---

## 👤 ავტორი

<div align="center">

**ნიკოლოზ კვარაცხელია**
პროგრამული უზრუნველყოფის შემქმნელი

[![GitHub](https://img.shields.io/badge/GitHub-nikolozkv-181717?style=for-the-badge&logo=github)](https://github.com/nikolozkv)

</div>

---

## ⚖️ საავტორო უფლებები და ლიცენზია

```
Copyright © 2026 ნიკოლოზ კვარაცხელია. ყველა უფლება დაცულია.

All Rights Reserved.

წინამდებარე პროგრამული უზრუნველყოფა და მისი კოდი წარმოადგენს
ავტორის ინტელექტუალურ საკუთრებას. ნებისმიერი სახის კოპირება,
გავრცელება, მოდიფიცირება, გამოყენება კომერციული ან არაკომერციული
მიზნებისთვის — მკაცრად აკრძალულია ავტორის წერილობითი ნებართვის გარეშე.

Unauthorized copying, distribution, modification, or use of this
software, in whole or in part, is strictly prohibited without the
express written permission of the author.
```

---

<div align="center">

*შექმნილია ❤️-ით · 2026 · თბილისი, საქართველო*

</div>
