using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModelPlugin : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            #region Эксперименты с фильтрами элементов
            //List<Wall> res1 = new FilteredElementCollector(doc)
            //    .OfCategory(BuiltInCategory.OST_Walls)
            //    .OfType<Wall>()
            //    .ToList();

            //var res1 = new FilteredElementCollector(doc)
            //    .OfClass(typeof(Wall))
            //    //.Cast<Wall>() // попытка приведения типа (возможны exception)
            //    .OfType<Wall>() // фильтрация
            //    .ToList();

            var res1 = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                //.Cast<Wall>() // попытка приведения типа (возможны exception)
                .OfType<WallType>() // фильтрация
                .ToList();

            //TaskDialog.Show("res1 QTY:", $"{res1.Count}");

            //var res2 = new FilteredElementCollector(doc)
            //    .OfCategory(BuiltInCategory.OST_Doors)
            //    .OfType<FamilyInstance>() // фильтрация
            //    .ToList();

            //string instData = String.Empty;
            //foreach (var cf in compFamilies)
            //    instData += cf.Id + Environment.NewLine;

            //var res2 = new FilteredElementCollector(doc)
            //    .OfCategory(BuiltInCategory.OST_Doors)
            //    //.Cast<Wall>() // попытка приведения типа (возможны exception)
            //    .OfType<FamilySymbol>() // фильтрация
            //    .ToList();

            //string typesData = String.Empty;
            //foreach (var cf in res2)
            //    typesData += cf.Id + Environment.NewLine;

            //TaskDialog.Show("Names:", $"{instData}{Environment.NewLine}{Environment.NewLine}{typesData}");

            var res2 = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                //.Cast<Wall>() // попытка приведения типа (возможны exception)
                .OfType<FamilyInstance>()
                .Where(x => x.Name.Equals("36\" x 84\""))
                .ToList(); // 10
            //TaskDialog.Show("res2 QTY:", $"{res2.Count}");

            var res3 = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType() //??
                .Where(x => x.Name.Equals("36\" x 84\""))
                .ToList(); // 11 (д.б.10??)
                           //TaskDialog.Show("res3 QTY:", $"{res3.Count}");
            #endregion

            int length = 30;
            int width = 18;

            List<Wall> walls = WallCreate(doc, length, width);

            Wall doorWall = walls[0];

            string doorType = "M_Single-Flush"; // ?избыточное?

            string doorSize = "0864 x 2032"; // общая часть типоразмера двери // EN:"0864 x 2032mm" // RU:"0864 x 2032 мм"

            FamilyInstance door = DoorCreate(doc, doorWall, doorSize);

            List<Wall> windowWalls = new List<Wall>();
            windowWalls.Add(walls[1]);
            windowWalls.Add(walls[2]);
            windowWalls.Add(walls[3]);

            string windowType = "M_Fixed"; // ?избыточное?

            string windowName = "0915 x 1220"; // общая часть типоразмера окна // EN:"0915 x 1220mm" // ..
            int winBaseHeightMM = 1000;
            foreach (var wall in windowWalls)
            {
                FamilyInstance window = WindowCreate(doc, wall, windowName, winBaseHeightMM);
            }

            //string roofTypeName = "Sloped Glazing";
            string roofTypeName = "Generic - 125mm";

            FootPrintRoof roof = PrintRoofCreate(doc, walls, roofTypeName);

            return Result.Succeeded;
        }

        public List<Wall> WallCreate(Document doc, int length, int width)
        {
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            Level lev1 = levels
                .Where(x => x.Name.Equals("Level 1") || x.Name.Equals("Уровень 1"))
                .SingleOrDefault();

            Level lev2 = levels
                .Where(x => x.Name.Equals("Level 2") || x.Name.Equals("Уровень 2"))
                .SingleOrDefault();

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(0, 0, 0));
            points.Add(new XYZ(length, 0, 0));
            points.Add(new XYZ(length, width, 0));
            points.Add(new XYZ(0, width, 0));
            points.Add(new XYZ(0, 0, 0));

            Transaction ts = new Transaction(doc, "Walls Creation Transaction");
            ts.Start();
            List<Wall> walls = new List<Wall>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, lev1.Id, false);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(lev2.Id);
                walls.Add(wall);
            }
            ts.Commit();
            return walls;
        }

        public FamilyInstance DoorCreate(Document doc, Wall wall, string doorSize)
        {
            LocationCurve doorLC = wall.Location as LocationCurve;
            XYZ doorLP = ((doorLC.Curve.GetEndPoint(0) + doorLC.Curve.GetEndPoint(1)) / 2);

            FamilySymbol doorFS = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.StartsWith(doorSize))
                //.Where(x => x.Family.Name.Equals(doorType)) // ?избыточное?
                .SingleOrDefault();

            Level doorLev = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .Where(x => x.Name.Equals(wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT)))
                .SingleOrDefault();

            if (doorFS == null)
                return null;
            else
            {
                Transaction ts2 = new Transaction(doc, "Door Creation Transaction");
                ts2.Start();
                if (!doorFS.IsActive)
                    doorFS.Activate();
                FamilyInstance door = doc.Create.NewFamilyInstance(doorLP, doorFS, wall, doorLev, StructuralType.NonStructural);
                ts2.Commit();
                return door;
            }
        }

        public FamilyInstance WindowCreate(Document doc, Wall wall, string winSize, int winLevMM)
        {
            LocationCurve windowLC = wall.Location as LocationCurve;
            XYZ windowLP = ((windowLC.Curve.GetEndPoint(0) + windowLC.Curve.GetEndPoint(1)) / 2);

            FamilySymbol windowFS = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.StartsWith(winSize))
                //.Where(x => x.Family.Name.Equals(windowType)) // ?избыточное?
                .SingleOrDefault();

            Level windowLev = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .Where(x => x.Name.Equals(wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT)))
                .SingleOrDefault();

            if (windowFS == null)
                return null;
            else
            {
                Transaction ts2 = new Transaction(doc, "Windows Creation Transaction");
                ts2.Start();
                if (!windowFS.IsActive)
                    windowFS.Activate();
                FamilyInstance window = doc.Create.NewFamilyInstance(windowLP, windowFS, wall, windowLev, StructuralType.NonStructural);
                window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(UnitUtils.ConvertToInternalUnits(winLevMM, UnitTypeId.Millimeters));
                ts2.Commit();
                return window;
            }
        }

        public FootPrintRoof PrintRoofCreate(Document doc, List<Wall> walls, string roofTypeName)
        {

            CurveArray footPrint = new CurveArray();
            foreach (Wall wall in walls)
            {
                LocationCurve windowLC = wall.Location as LocationCurve;
                footPrint.Append(windowLC.Curve);
            }

            Level roofLev = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                //.Where(x => x.Name.Equals(walls[0].get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).ToString()))
                .Where(x => x.Name.Equals(doc.GetElement(walls[0].get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId()).Name.ToString()))
                .SingleOrDefault();

            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x => x.Name.Equals(roofTypeName))
                .SingleOrDefault();

            ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();

            Transaction ts3 = new Transaction(doc, "Roof Create Transaction");
            ts3.Start();
            FootPrintRoof roof = doc.Create.NewFootPrintRoof(footPrint, roofLev, roofType, out footPrintToModelCurveMapping);
            foreach (ModelCurve mc in footPrintToModelCurveMapping)
            {
                roof.set_DefinesSlope(mc, true);
                roof.set_SlopeAngle(mc, 0.5);
            }
            ts3.Commit();
            return roof;
        }
    }    
}