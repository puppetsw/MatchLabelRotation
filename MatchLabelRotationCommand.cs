using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

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

		[CommandMethod("MatchLabelRotation", CommandFlags.Modal)]
		public void MatchLabelRotation()
		{
			var peo = new PromptEntityOptions("\nSelect CogoPoint label to match rotation: ");
			peo.SetRejectMessage("\nSelected entity is not a CogoPoint label.");
			// Assuming CogoPoint is a type from Civil 3D API
			peo.AddAllowedClass(typeof(Autodesk.Civil.DatabaseServices.CogoPoint), false);
			var per = _editor.GetEntity(peo);
			if (per.Status != PromptStatus.OK)
				return;
			ObjectId cogoPointId = per.ObjectId;
			var pso = new PromptSelectionOptions
			{
				MessageForAdding = "\nSelect CogoPoint objects to rotate: "
			};
			var psr = _editor.GetSelection(pso);
			if (psr.Status != PromptStatus.OK)
				return;
			using (var transactAndForget = new TransactAndForget(true))
			{
				var cogoPoint = transactAndForget.GetObject<Autodesk.Civil.DatabaseServices.CogoPoint>(cogoPointId, OpenMode.ForRead);
				double labelRotation = cogoPoint.LabelRotation;
				foreach (SelectedObject selectedObject in psr.Value)
				{
					if (selectedObject == null) continue;
					var entity = transactAndForget.GetObject<Autodesk.Civil.DatabaseServices.CogoPoint>(selectedObject.ObjectId, OpenMode.ForWrite);
					entity.LabelRotation = labelRotation;
				}
			}
		}
	}
}
