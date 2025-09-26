using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

namespace MatchLabelRotation
{
    public class MatchLabelRotationCommand
    {
        private readonly Editor _editor;
        private readonly Database _database;

        public MatchLabelRotationCommand()
        {
            _editor = Application.DocumentManager.MdiActiveDocument.Editor;
            _database = Application.DocumentManager.MdiActiveDocument.Database;
        }

        [CommandMethod("MatchLabelRotation", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void MatchLabelRotation()
        {
            ObjectId basePointId = default;

            if (TryGetImpliedSelectionOfType<CogoPoint>(out var basePoint))
            {
                if (basePoint.Count == 1)
                {
                    basePointId = basePoint[0];
                }
            }
            else
            {
                if (!TryGetEntityOfType<CogoPoint>("\nSelect source CogoPoint: ", out basePointId))
                {
                    return;
                }
            }

            using (var transactAndForget = new TransactAndForget(true))
            {
                var baseCogoPoint = transactAndForget.GetObject<CogoPoint>(basePointId, OpenMode.ForRead);
                double labelRotation = baseCogoPoint.LabelRotation;

                do
                {
                    if (!TryGetEntityOfType<CogoPoint>("\nSelect destination CogoPoint: ", out var pointId))
                    {
                        return;
                    }

                    var entity = transactAndForget.GetObject<CogoPoint>(pointId, OpenMode.ForWrite);
                    entity.LabelRotation = labelRotation;
                    _editor.Regen();
                } while (true);
            }
        }

        /// <summary>
        /// Gets a implied selection of type T.
        /// </summary>
        /// <param name="objectIds">Collection of <see cref="ObjectId"/>s obtained from the selection set.</param>
        /// <typeparam name="T">Type of <see cref="Autodesk.AutoCAD.DatabaseServices.Entity"/></typeparam>
        /// <returns><c>true</c> if the selection was successful, otherwise <c>false</c>.</returns>
        /// <remarks>Will filter out any entities not of type T.</remarks>
        public bool TryGetImpliedSelectionOfType<T>(out ObjectIdCollection objectIds) where T : Entity
        {
            var psr = _editor.SelectImplied();
            objectIds = new ObjectIdCollection();

            if (psr.Status != PromptStatus.OK)
                return false;

            var entityType = RXObject.GetClass(typeof(T));
            foreach (var objectId in psr.Value.GetObjectIds())
            {
                // check that the objectId type matches the entityType
                if (objectId.ObjectClass.Equals(entityType))
                {
                    objectIds.Add(objectId);
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the type of the entities of.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="addMessage">The add message.</param>
        /// <param name="removeMessage">The remove message.</param>
        /// <param name="objectIds">The object ids.</param>
        /// <returns><c>true</c> if successfully got a selection, <c>false</c> otherwise.</returns>
        public bool TryGetSelectionOfType<T>(string addMessage, string removeMessage, out ObjectIdCollection objectIds) where T : Entity
        {
            var entityType = RXObject.GetClass(typeof(T));

            objectIds = new ObjectIdCollection();

            TypedValue[] typedValues = { new TypedValue((int)DxfCode.Start, entityType.DxfName) };
            var ss = new SelectionFilter(typedValues);
            var pso = new PromptSelectionOptions
            {
                MessageForAdding = addMessage,
                MessageForRemoval = removeMessage
            };

            var result = _editor.GetSelection(pso, ss);

            if (result.Status != PromptStatus.OK)
                return false;

            objectIds = new ObjectIdCollection(result.Value.GetObjectIds());

            return true;
        }

        /// <summary>
        /// Prompts the user to select a single entity of type T using GetEntity.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Autodesk.AutoCAD.DatabaseServices.Entity"/></typeparam>
        /// <param name="promptMessage">The prompt message to display.</param>
        /// <param name="objectId">The selected object's ObjectId if successful.</param>
        /// <returns><c>true</c> if an entity of type T was selected, <c>false</c> otherwise.</returns>
        public bool TryGetEntityOfType<T>(string promptMessage, out ObjectId objectId) where T : Entity
        {
            objectId = ObjectId.Null;
            var entityType = RXObject.GetClass(typeof(T));
            var peo = new PromptEntityOptions(promptMessage)
            {
                AllowNone = false
            };
            peo.SetRejectMessage($"\nOnly {entityType.DxfName} entities are allowed.");
            peo.AddAllowedClass(typeof(T), exactMatch: true);

            var result = _editor.GetEntity(peo);

            if (result.Status != PromptStatus.OK)
                return false;

            objectId = result.ObjectId;
            return true;
        }
    }
}
