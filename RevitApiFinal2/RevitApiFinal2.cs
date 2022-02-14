using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitApiFinal2
{
    #region Плагин для вставки марок помещений
    [TransactionAttribute(TransactionMode.Manual)]
    public class RevitApiFinal : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            RoomPickFilter roomPickFilter = new RoomPickFilter();
            IList<Reference> rooms = uidoc.Selection.PickObjects(ObjectType.Element,roomPickFilter, "Выберите комнаты"); //пользователь выбирает комнаты
            List<ElementId> roomids = (from Reference r in rooms select r.ElementId).ToList(); //получаем список Id комнат

            var roomTagTypes = new FilteredElementCollector(doc)
                       .OfCategory(BuiltInCategory.OST_RoomTags)
                       .OfType<FamilySymbol>()
                       .FirstOrDefault();

            View view = new FilteredElementCollector(doc) //выбираем вид
                .OfClass(typeof(View))
                .OfType<View>()
                .Where(x => x.Name.Equals("Level 1"))
                .FirstOrDefault();

            Transaction transaction = new Transaction(doc, "Марки помещений");
            transaction.Start();
            foreach (ElementId roomid in roomids)
            {
                Element e = doc.GetElement(roomid);
                Room r = e as Room;
                XYZ cen = GetElementCenter(r);
                UV center = new UV(cen.X, cen.Y);
                doc.Create.NewRoomTag(new LinkElementId(roomid), center, view.Id);
            }

            transaction.Commit();

            return Result.Succeeded;
        }
        public XYZ GetElementCenter(Element element) //метод, получающий элемент и возвращающий точку
        {
            BoundingBoxXYZ boundin = element.get_BoundingBox(null); //"рамка" вокруг группы. В BoundingBoxXYZ min - левый нижний дальний угол, а max - правый верхний ближний
            return (boundin.Min + boundin.Max) / 2;
        }
        public class RoomPickFilter : ISelectionFilter //фильтр выбора только по помещениям
        {
            public bool AllowElement(Element e)
            {
                return (e.Category.Id.IntegerValue.Equals(
                (int)BuiltInCategory.OST_Rooms));
            }
            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }
    #endregion
}
