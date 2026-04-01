using System;
using System.Collections.Generic;

namespace InventoryManagementWebApp.Models
{
    public class OperationsSpiritPageViewModel
    {
        // კომპანიის დეტალები
        public int BarrelID { get; set; }
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string CompanyTypeName { get; set; }
        public string CompanyLot { get; set; }

        // სასმელის დეტალები
        public int BeverageID { get; set; }
        public string BeverageName { get; set; }
        public string ProductType { get; set; }
        public string Category { get; set; }
        public string Color { get; set; }
        public string Sweetness { get; set; }

        // კასრის მიმდინარე მდგომარეობა (სპირტის სპეციფიკა)
        public decimal CurrentVolume { get; set; }
        public DateTime? WeightedAvgDate { get; set; } // ✅ სპირტის ასაკის ათვლის წერტილი
        public decimal? CurrentAlcPercent { get; set; } // ✅ ალკოჰოლის პროცენტი

        // ოპერაციების ისტორია
        // (აქ შეგვიძლია გამოვიყენოთ იგივე OperationItemViewModel, რაც ღვინოში გაქვს, 
        // რადგან ისტორიის ცხრილში ველები მეტწილად იდენტურია)
        public List<OperationItemViewModel> Operations { get; set; }
    }
}