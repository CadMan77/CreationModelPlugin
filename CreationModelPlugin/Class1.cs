using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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
                .Where(x=>x.Name.Equals("36\" x 84\""))
                .ToList(); // 10
            //TaskDialog.Show("res2 QTY:", $"{res2.Count}");

            var res3 = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType() //??
                .Where(x=>x.Name.Equals("36\" x 84\""))
                .ToList(); // 11 (д.б.10??)
            //TaskDialog.Show("res3 QTY:", $"{res3.Count}");
            #endregion

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();
                              
            Level lev1 = levels
                .Where(x=>x.Name.Equals("Level 1")) // ?билингво? ~ ||"Уровень 1"
                .FirstOrDefault();
                //.First(); //?

            Level lev2 = levels
                .Where(x => x.Name.Equals("Level 2"))
                .FirstOrDefault();

            int length = 30;
            int width = 18;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(0, 0, 0));
            points.Add(new XYZ(length, 0, 0));
            points.Add(new XYZ(length, width, 0));
            points.Add(new XYZ(0, width, 0));
            points.Add(new XYZ(0, 0, 0));

            List<Wall> walls = new List<Wall>();

            Transaction ts = new Transaction(doc, "Walls Creation Transaction");
            ts.Start();
            for (int i = 0; i < points.Count-1; i++)
            {
                Line line = Line.CreateBound(points[i], points[i+1]);
                Wall wall = Wall.Create(doc, line, lev1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(lev2.Id);
            }
            ts.Commit();
            return Result.Succeeded;
        }
    }
}
