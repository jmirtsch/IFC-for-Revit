﻿//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2013  Autodesk, Inc.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Exporter;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Exporter.PropertySet;

namespace Revit.IFC.Export.Utility
{
    /// <summary>
    /// Provides static methods to create openings.
    /// </summary>
    class OpeningUtil
    {
        /// <summary>
        /// Creates openings if there is necessary.
        /// </summary>
        /// <param name="elementHandle">The element handle to create openings.</param>
        /// <param name="element">The element to create openings.</param>
        /// <param name="info">The extrusion data.</param>
        /// <param name="extraParams">The extrusion creation data.</param>
        /// <param name="offsetTransform">The offset transform from ExportBody, or the identity transform.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="originalPlacement">The original placement handle.</param>
        /// <param name="setter">The PlacementSetter.</param>
        /// <param name="wrapper">The ProductWrapper.</param>
        private static void CreateOpeningsIfNecessaryBase(IFCAnyHandle elementHandle, Element element, IList<IFCExtrusionData> info,
           IFCExtrusionCreationData extraParams, Transform offsetTransform, ExporterIFC exporterIFC,
           IFCAnyHandle originalPlacement, PlacementSetter setter, ProductWrapper wrapper)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(elementHandle))
                return;

            int sz = info.Count;
            if (sz == 0)
                return;

            using (TransformSetter transformSetter = TransformSetter.Create())
            {
                if (offsetTransform != null)
                    transformSetter.Initialize(exporterIFC, offsetTransform.Inverse);

                IFCFile file = exporterIFC.GetFile();
                ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);
                Document document = element.Document;

                string openingObjectType = "Opening";

                int openingNumber = 1;
                for (int curr = info.Count - 1; curr >= 0; curr--)
                {
                    IFCAnyHandle extrusionHandle = ExtrusionExporter.CreateExtrudedSolidFromExtrusionData(exporterIFC, element, info[curr]);
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(extrusionHandle))
                        continue;

                    IFCAnyHandle styledItemHnd = BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, document,
                        extrusionHandle, ElementId.InvalidElementId);

                    HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                    bodyItems.Add(extrusionHandle);

                    IFCAnyHandle contextOfItems = exporterIFC.Get3DContextHandle("Body");
                    IFCAnyHandle bodyRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, element, categoryId, contextOfItems, bodyItems, null);
                    IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                    representations.Add(bodyRep);

                    IFCAnyHandle openingRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);

                    IFCAnyHandle openingPlacement = ExporterUtil.CopyLocalPlacement(file, originalPlacement);
                    string guid = GUIDUtil.CreateGUID();
                    IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                    string openingName = NamingUtil.GetIFCNamePlusIndex(element, openingNumber++);
                    string elementId = NamingUtil.CreateIFCElementId(element);
                    IFCAnyHandle openingElement = IFCInstanceExporter.CreateOpeningElement(file, guid, ownerHistory,
                       openingName, null, openingObjectType, openingPlacement, openingRep, elementId);
                    wrapper.AddElement(openingElement, setter, extraParams, true);
                    if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities && (extraParams != null))
                        PropertyUtil.CreateOpeningQuantities(exporterIFC, openingElement, extraParams);

                    string voidGuid = GUIDUtil.CreateGUID();
                    IFCInstanceExporter.CreateRelVoidsElement(file, voidGuid, ownerHistory, null, null, elementHandle, openingElement);
                }
            }
        }

        /// <summary>
        /// Creates openings associated with an extrusion, if there are any.
        /// </summary>
        /// <param name="elementHandle">The element handle to create openings.</param>
        /// <param name="element">The element to create openings.</param>
        /// <param name="info">The extrusion data.</param>
        /// <param name="offsetTransform">The offset transform from ExportBody, or the identity transform.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="originalPlacement">The original placement handle.</param>
        /// <param name="setter">The PlacementSetter.</param>
        /// <param name="wrapper">The ProductWrapper.</param>
        public static void CreateOpeningsIfNecessary(IFCAnyHandle elementHandle, Element element, IList<IFCExtrusionData> info, Transform offsetTransform,
           ExporterIFC exporterIFC, IFCAnyHandle originalPlacement,
           PlacementSetter setter, ProductWrapper wrapper)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(elementHandle))
                return;

            using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
            {
                CreateOpeningsIfNecessaryBase(elementHandle, element, info, extraParams, offsetTransform, exporterIFC, originalPlacement, setter, wrapper);
            }
        }

        /// <summary>
        /// Creates openings associated with an extrusion, if there are any.
        /// </summary>
        /// <param name="elementHandle">The element handle to create openings.</param>
        /// <param name="element">The element to create openings.</param>
        /// <param name="extraParams">The extrusion creation data.</param>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="originalPlacement">The original placement handle.</param>
        /// <param name="setter">The PlacementSetter.</param>
        /// <param name="wrapper">The ProductWrapper.</param>
        public static void CreateOpeningsIfNecessary(IFCAnyHandle elementHandle, Element element, IFCExtrusionCreationData extraParams,
           Transform offsetTransform, ExporterIFC exporterIFC, IFCAnyHandle originalPlacement,
           PlacementSetter setter, ProductWrapper wrapper)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(elementHandle))
                return;

            ElementId categoryId = CategoryUtil.GetSafeCategoryId(element);

            IList<IFCExtrusionData> info = extraParams.GetOpenings();
            CreateOpeningsIfNecessaryBase(elementHandle, element, info, extraParams, offsetTransform, exporterIFC, originalPlacement, setter, wrapper);
            extraParams.ClearOpenings();
        }

        public static bool NeedToCreateOpenings(IFCAnyHandle elementHandle, IFCExtrusionCreationData extraParams)
        {
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(elementHandle))
                return false;

            if (extraParams == null)
                return false;

            IList<IFCExtrusionData> info = extraParams.GetOpenings();
            return (info.Count > 0);
        }

        /// <summary>
        /// Finds parent handle from opening CurveLoop and parent CurveLoops.
        /// </summary>
        /// <param name="elementHandles">The parent handles.</param>
        /// <param name="curveLoops">The parent CurveLoops.</param>
        /// <param name="openingLoop">The opening CurveLoop.</param>
        /// <returns>The parent handle.</returns>
        private static IFCAnyHandle FindParentHandle(IList<IFCAnyHandle> elementHandles, IList<CurveLoop> curveLoops, CurveLoop openingLoop)
        {
            // first one is roof handle, others are slabs
            if (elementHandles.Count != curveLoops.Count + 1)
                return null;

            for (int ii = 0; ii < curveLoops.Count; ii++)
            {
                if (GeometryUtil.CurveLoopsInside(openingLoop, curveLoops[ii]) || GeometryUtil.CurveLoopsIntersect(openingLoop, curveLoops[ii]))
                {
                    return elementHandles[ii + 1];
                }
            }
            return elementHandles[0];
        }

        /// <summary>
        /// Adds openings to an element.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="elementHandles">The parent handles.</param>
        /// <param name="curveLoops">The parent CurveLoops.</param>
        /// <param name="element">The element.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="scaledWidth">The width.</param>
        /// <param name="range">The range.</param>
        /// <param name="setter">The placement setter.</param>
        /// <param name="localPlacement">The local placement.</param>
        /// <param name="localWrapper">The wrapper.</param>
        public static void AddOpeningsToElement(ExporterIFC exporterIFC, IList<IFCAnyHandle> elementHandles, IList<CurveLoop> curveLoops, Element element, Plane plane, double scaledWidth,
            IFCRange range, PlacementSetter setter, IFCAnyHandle localPlacement, ProductWrapper localWrapper)
        {
            IList<IFCOpeningData> openingDataList = ExporterIFCUtils.GetOpeningData(exporterIFC, element, plane, range);
            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
            foreach (IFCOpeningData openingData in openingDataList)
            {
                Element openingElem = element.Document.GetElement(openingData.OpeningElementId);
                if (openingElem == null)
                    openingElem = element;

                IList<IFCExtrusionData> extrusionDataList = openingData.GetExtrusionData();
                IFCAnyHandle parentHandle = null;
                if (elementHandles.Count > 1 && extrusionDataList.Count > 0)
                {
                    parentHandle = FindParentHandle(elementHandles, curveLoops, extrusionDataList[0].GetLoops()[0]);
                }

                if (parentHandle == null)
                    parentHandle = elementHandles[0];

                bool isDoorOrWindowOpening = IsDoorOrWindowOpening(exporterIFC, openingElem, element);
                if (isDoorOrWindowOpening)
                {
                    DoorWindowDelayedOpeningCreator delayedCreator = DoorWindowDelayedOpeningCreator.Create(exporterIFC, openingData, scaledWidth, element.Id, parentHandle);
                    if (delayedCreator != null)
                    {
                        ExporterCacheManager.DoorWindowDelayedOpeningCreatorCache.Add(delayedCreator);
                        continue;
                    }
                }

                bool canUseElementGUID = !isDoorOrWindowOpening;

                IList<Solid> solids = openingData.GetOpeningSolids();
                foreach (Solid solid in solids)
                {
                    using (IFCExtrusionCreationData extrusionCreationData = new IFCExtrusionCreationData())
                    {
                        extrusionCreationData.SetLocalPlacement(ExporterUtil.CreateLocalPlacement(file, localPlacement, null));
                        extrusionCreationData.ReuseLocalPlacement = true;

                        string openingGUID = null;
                        if (canUseElementGUID)
                        {
                            openingGUID = GUIDUtil.CreateGUID(openingElem);
                            canUseElementGUID = false;
                        }
                        else
                            openingGUID = GUIDUtil.CreateGUID();
                        CreateOpening(exporterIFC, parentHandle, element, openingElem, openingGUID, solid, scaledWidth, openingData.IsRecess, extrusionCreationData, setter, localWrapper);
                    }
                }

                foreach (IFCExtrusionData extrusionData in extrusionDataList)
                {
                    if (extrusionData.ScaledExtrusionLength < MathUtil.Eps())
                        extrusionData.ScaledExtrusionLength = scaledWidth;

                    string openingGUID = null;
                    if (canUseElementGUID)
                    {
                        openingGUID = GUIDUtil.CreateGUID(element);
                        canUseElementGUID = false;
                    }
                    else
                        openingGUID = GUIDUtil.CreateGUID();
                    CreateOpening(exporterIFC, parentHandle, localPlacement, element, openingElem, openingGUID, extrusionData, plane, openingData.IsRecess, setter, localWrapper);
                }
            }
        }

        /// <summary>
        /// Adds openings to an element.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="elementHandle">The parent handle.</param>
        /// <param name="element">The element.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="scaledWidth">The width.</param>
        /// <param name="range">The range.</param>
        /// <param name="setter">The placement setter.</param>
        /// <param name="localPlacement">The local placement.</param>
        /// <param name="localWrapper">The wrapper.</param>
        public static void AddOpeningsToElement(ExporterIFC exporterIFC, IFCAnyHandle elementHandle, Element element, Plane plane, double scaledWidth,
            IFCRange range, PlacementSetter setter, IFCAnyHandle localPlacement, ProductWrapper localWrapper)
        {
            IList<IFCAnyHandle> elementHandles = new List<IFCAnyHandle>();
            elementHandles.Add(elementHandle);
            AddOpeningsToElement(exporterIFC, elementHandles, null, element, plane, scaledWidth, range, setter, localPlacement, localWrapper);
        }

        /// <summary>
        /// Creates an opening from a solid.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="hostObjHnd">The host object handle.</param>
        /// <param name="hostElement">The host element.</param>
        /// <param name="insertElement">The insert element.</param>
        /// <param name="openingGUID">The GUID for the opening, depending on how the opening is created.</param>
        /// <param name="solid">The solid.</param>
        /// <param name="scaledHostWidth">The scaled host width.</param>
        /// <param name="isRecess">True if it is recess.</param>
        /// <param name="extrusionCreationData">The extrusion creation data.</param>
        /// <param name="setter">The placement setter.</param>
        /// <param name="localWrapper">The product wrapper.</param>
        /// <returns>The created opening handle.</returns>
        static public IFCAnyHandle CreateOpening(ExporterIFC exporterIFC, IFCAnyHandle hostObjHnd, Element hostElement, Element insertElement, string openingGUID,
            Solid solid, double scaledHostWidth, bool isRecess, IFCExtrusionCreationData extrusionCreationData, PlacementSetter setter, ProductWrapper localWrapper)
        {
            IFCFile file = exporterIFC.GetFile();

            ElementId catId = CategoryUtil.GetSafeCategoryId(insertElement);

            XYZ prepToWall;
            bool isLinearWall = GetOpeningDirection(hostElement, out prepToWall);
            if (isLinearWall)
            {
                extrusionCreationData.CustomAxis = prepToWall;
                extrusionCreationData.PossibleExtrusionAxes = IFCExtrusionAxes.TryCustom;
            }

            BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true);
            BodyData bodyData = BodyExporter.ExportBody(exporterIFC, insertElement, catId, ElementId.InvalidElementId,
                solid, bodyExporterOptions, extrusionCreationData);

            IFCAnyHandle openingRepHnd = bodyData.RepresentationHnd;
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(openingRepHnd))
            {
                extrusionCreationData.ClearOpenings();
                return null;
            }
            IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
            representations.Add(openingRepHnd);
            IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);

            IFCAnyHandle openingPlacement = extrusionCreationData.GetLocalPlacement();
            IFCAnyHandle hostObjPlacementHnd = IFCAnyHandleUtil.GetObjectPlacement(hostObjHnd);
            Transform relTransform = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(openingPlacement, hostObjPlacementHnd);

            openingPlacement = ExporterUtil.CreateLocalPlacement(file, hostObjPlacementHnd,
                relTransform.Origin, relTransform.BasisZ, relTransform.BasisX);

            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
            double scaledOpeningLength = extrusionCreationData.ScaledLength;
            string openingObjectType = "Opening";
            if (!MathUtil.IsAlmostZero(scaledHostWidth) && !MathUtil.IsAlmostZero(scaledOpeningLength))
            {
                 openingObjectType = scaledOpeningLength < (scaledHostWidth - MathUtil.Eps()) ? "Recess" : "Opening";
            }
            else
            {
                openingObjectType = isRecess ? "Recess" : "Opening";
            }
            string openingName = NamingUtil.GetNameOverride(insertElement, null);
            if (string.IsNullOrEmpty(openingName))
                openingName = NamingUtil.GetNameOverride(hostElement, NamingUtil.CreateIFCObjectName(exporterIFC, hostElement));
            IFCAnyHandle openingHnd = IFCInstanceExporter.CreateOpeningElement(file, openingGUID, ownerHistory, openingName, null,
                openingObjectType, openingPlacement, prodRep, null);
            if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
                PropertyUtil.CreateOpeningQuantities(exporterIFC, openingHnd, extrusionCreationData);

            if (localWrapper != null)
                localWrapper.AddElement(openingHnd, setter, extrusionCreationData, true);

            string voidGuid = GUIDUtil.CreateGUID();
            IFCInstanceExporter.CreateRelVoidsElement(file, voidGuid, ownerHistory, null, null, hostObjHnd, openingHnd);
            return openingHnd;
        }

        /// <summary>
        /// Creates an opening from extrusion data.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="hostObjHnd">The host handle.</param>
        /// <param name="hostPlacement">The host placement.</param>
        /// <param name="hostElement">The host element.</param>
        /// <param name="insertElement">The opening element.</param>
        /// <param name="openingGUID">The opening GUID.</param>
        /// <param name="extrusionData">The extrusion data.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="isRecess">True if it is a recess.</param>
        /// <returns>The opening handle.</returns>
        static public IFCAnyHandle CreateOpening(ExporterIFC exporterIFC, IFCAnyHandle hostObjHnd, IFCAnyHandle hostPlacement, Element hostElement, 
            Element insertElement, string openingGUID, IFCExtrusionData extrusionData, Plane plane, bool isRecess, 
            PlacementSetter setter, ProductWrapper localWrapper)
        {
            IFCFile file = exporterIFC.GetFile();

            IList<CurveLoop> curveLoops = extrusionData.GetLoops();

            if (curveLoops.Count == 0)
                return null;

            if (plane == null)
            {
                // assumption: first curve loop defines the plane.
                if (!curveLoops[0].HasPlane())
                    return null;
                plane = curveLoops[0].GetPlane();
            }

            ElementId catId = CategoryUtil.GetSafeCategoryId(insertElement);

            IFCAnyHandle openingProdRepHnd = RepresentationUtil.CreateExtrudedProductDefShape(exporterIFC, insertElement, catId,
                curveLoops, plane, extrusionData.ExtrusionDirection, extrusionData.ScaledExtrusionLength);

            string openingObjectType = isRecess ? "Recess" : "Opening";
            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
            string openingName = NamingUtil.GetNameOverride(insertElement, null);
            if (string.IsNullOrEmpty(openingName))
                openingName = NamingUtil.GetNameOverride(hostElement, NamingUtil.CreateIFCObjectName(exporterIFC, hostElement));
            IFCAnyHandle openingHnd = IFCInstanceExporter.CreateOpeningElement(file, openingGUID, ownerHistory, openingName, null,
                openingObjectType, ExporterUtil.CreateLocalPlacement(file, hostPlacement, null), openingProdRepHnd, null);
            IFCExtrusionCreationData ecData = null;
            if (ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities)
            {
                double height, width;
                ecData = new IFCExtrusionCreationData();
                if (GeometryUtil.ComputeHeightWidthOfCurveLoop(curveLoops[0], plane, out height, out width))
                {
                    ecData.ScaledHeight = UnitUtil.ScaleLength(height);
                    ecData.ScaledWidth = UnitUtil.ScaleLength(width);
                }
                else
                {
                    double area = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoops);
                    ecData.ScaledArea = UnitUtil.ScaleArea(area);
                }
                PropertyUtil.CreateOpeningQuantities(exporterIFC, openingHnd, ecData);
            }

            if (localWrapper != null)
                localWrapper.AddElement(openingHnd, setter, ecData, true);

            string voidGuid = GUIDUtil.CreateGUID();
            IFCInstanceExporter.CreateRelVoidsElement(file, voidGuid, ownerHistory, null, null, hostObjHnd, openingHnd);
            return openingHnd;
        }

        /// <summary>
        /// Checks if it is a door or window opening for a wall.
        /// </summary>
        /// <param name="exporterIFC">The exporter.</param>
        /// <param name="openingElem">The opening element.</param>
        /// <param name="hostElement">The host element.</param>
        /// <returns>True if it is a door or window opening for a wall.</returns>
        public static bool IsDoorOrWindowOpening(ExporterIFC exporterIFC, Element openingElem, Element hostElement)
        {
            if (openingElem is FamilyInstance && hostElement is Wall)
            {
                string ifcEnumType;
                IFCExportType exportType = ExporterUtil.GetExportType(exporterIFC, openingElem, out ifcEnumType);
                Element instHost = (openingElem as FamilyInstance).Host;
                return (exportType == IFCExportType.IfcDoorType || exportType == IFCExportType.IfcWindowType) &&
                    (instHost != null && instHost.Id == hostElement.Id);
            }

            return false;
        }

        static bool GetOpeningDirection(Element hostElem, out XYZ perpToWall)
        {
            bool isLinearWall = false;
            perpToWall = new XYZ(0, 0, 0);
            Wall wall = hostElem as Wall;
            if (wall != null)
            {
                Curve curve = WallExporter.GetWallAxis(wall);
                if (curve is Line)
                {
                    isLinearWall = true;
                    XYZ wallDir = (curve as Line).Direction;
                    perpToWall = XYZ.BasisZ.CrossProduct(wallDir);
                }
            }

            return isLinearWall;
        }
    }
}
