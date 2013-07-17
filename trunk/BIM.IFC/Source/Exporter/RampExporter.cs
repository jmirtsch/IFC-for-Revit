﻿//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using BIM.IFC.Exporter.PropertySet;
using BIM.IFC.Utility;
using BIM.IFC.Toolkit;

namespace BIM.IFC.Exporter
{
    /// <summary>
    /// Provides methods to export ramps
    /// </summary>
    class RampExporter
    {
        /// <summary>
        /// Checks if exporting an element of Ramp category.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>True if element is of category OST_Ramps.</returns>
        static public bool IsRamp(Element element)
        {
            // FaceWall should be exported as IfcWall.
            return (CategoryUtil.GetSafeCategoryId(element) == new ElementId(BuiltInCategory.OST_Ramps));
        }

        static private double GetDefaultHeightForRamp(double scale)
        {
            // The default height for ramps is 3'.
            return (3.0 * scale);
        }

        /// <summary>
        /// Gets the ramp height for a ramp.
        /// </summary>
        /// <param name="exporterIFC">
        /// The exporter.
        /// </param>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <returns>
        /// The unscaled height.
        /// </returns>
        static public double GetRampHeight(ExporterIFC exporterIFC, Element element)
        {
            // Re-use the code for stairs height for legacy stairs.
            return StairsExporter.GetStairsHeightForLegacyStair(exporterIFC, element, GetDefaultHeightForRamp(exporterIFC.LinearScale));
        }

        /// <summary>
        /// Gets the number of flights of a multi-story ramp.
        /// </summary>
        /// <param name="exporterIFC">
        /// The exporter.
        /// </param>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <returns>
        /// The number of flights (at least 1.)
        /// </returns>
        static public int GetNumFlightsForRamp(ExporterIFC exporterIFC, Element element)
        {
            return StairsExporter.GetNumFlightsForLegacyStair(exporterIFC, element, GetDefaultHeightForRamp(exporterIFC.LinearScale));
        }

        /// <summary>
        /// Gets IFCRampType from ramp type name.
        /// </summary>
        /// <param name="rampTypeName">The ramp type name.</param>
        /// <returns>The IFCRampType.</returns>
        public static IFCRampType GetIFCRampType(string rampTypeName)
        {
            string typeName = NamingUtil.RemoveSpacesAndUnderscores(rampTypeName);

            if (String.Compare(typeName, "StraightRun", true) == 0 ||
                String.Compare(typeName, "StraightRunRamp", true) == 0)
                return Toolkit.IFCRampType.Straight_Run_Ramp;
            if (String.Compare(typeName, "TwoStraightRun", true) == 0 ||
                String.Compare(typeName, "TwoStraightRunRamp", true) == 0)
                return Toolkit.IFCRampType.Two_Straight_Run_Ramp;
            if (String.Compare(typeName, "QuarterTurn", true) == 0 ||
                String.Compare(typeName, "QuarterTurnRamp", true) == 0)
                return Toolkit.IFCRampType.Quarter_Turn_Ramp;
            if (String.Compare(typeName, "TwoQuarterTurn", true) == 0 ||
                String.Compare(typeName, "TwoQuarterTurnRamp", true) == 0)
                return Toolkit.IFCRampType.Two_Quarter_Turn_Ramp;
            if (String.Compare(typeName, "HalfTurn", true) == 0 ||
                String.Compare(typeName, "HalfTurnRamp", true) == 0)
                return Toolkit.IFCRampType.Half_Turn_Ramp;
            if (String.Compare(typeName, "Spiral", true) == 0 ||
                String.Compare(typeName, "SpiralRamp", true) == 0)
                return Toolkit.IFCRampType.Spiral_Ramp;
            if (String.Compare(typeName, "UserDefined", true) == 0)
                return Toolkit.IFCRampType.UserDefined;

            return Toolkit.IFCRampType.NotDefined;
        }

        /// <summary>
        /// Exports the top stories of a multistory ramp.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="ramp">The ramp element.</param>
        /// <param name="numFlights">The number of flights for a multistory ramp.</param>
        /// <param name="rampHnd">The stairs container handle.</param>
        /// <param name="components">The components handles.</param>
        /// <param name="ecData">The extrusion creation data.</param>
        /// <param name="componentECData">The extrusion creation data for the components.</param>
        /// <param name="placementSetter">The placement setter.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportMultistoryRamp(ExporterIFC exporterIFC, Element ramp, int numFlights,
            IFCAnyHandle rampHnd, IList<IFCAnyHandle> components, IList<IFCExtrusionCreationData> componentECData,
            IFCPlacementSetter placementSetter, ProductWrapper productWrapper)
        {
            if (numFlights < 2)
                return;

            double scale = exporterIFC.LinearScale;

            double heightNonScaled = GetRampHeight(exporterIFC, ramp);
            if (heightNonScaled < MathUtil.Eps())
                return;

            if (IFCAnyHandleUtil.IsNullOrHasNoValue(rampHnd))
                return;

            IFCAnyHandle localPlacement = IFCAnyHandleUtil.GetObjectPlacement(rampHnd);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(localPlacement))
                return;

            IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();

            IFCFile file = exporterIFC.GetFile();

            IFCAnyHandle relPlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
            IFCAnyHandle ptHnd = IFCAnyHandleUtil.GetLocation(relPlacement);
            IList<double> origCoords = IFCAnyHandleUtil.GetCoordinates(ptHnd);

            IList<IFCAnyHandle> rampLocalPlacementHnds = new List<IFCAnyHandle>();
            IList<IFCLevelInfo> levelInfos = new List<IFCLevelInfo>();
            for (int ii = 0; ii < numFlights - 1; ii++)
            {
                double newOffsetScaled = 0.0;
                IFCAnyHandle newLevelHnd = null;
                levelInfos.Add(
                    placementSetter.GetOffsetLevelInfoAndHandle(heightNonScaled * (ii + 1), scale, out newLevelHnd, out newOffsetScaled));
                if (levelInfos[ii] == null)
                    levelInfos[ii] = placementSetter.GetLevelInfo();

                XYZ orig;
                if (ptHnd.HasValue)
                {
                    orig = new XYZ(origCoords[0], origCoords[1], newOffsetScaled);
                }
                else
                {
                    orig = new XYZ(0.0, 0.0, newOffsetScaled);
                }
                IFCAnyHandle relativePlacementHnd = ExporterUtil.CreateAxis2Placement3D(file, orig);
                rampLocalPlacementHnds.Add(IFCInstanceExporter.CreateLocalPlacement(file, newLevelHnd, relativePlacementHnd));
            }

            IList<List<IFCAnyHandle>> newComponents = new List<List<IFCAnyHandle>>();
            for (int ii = 0; ii < numFlights - 1; ii++)
                newComponents.Add(new List<IFCAnyHandle>());

            int compIdx = 0;
            ElementId catId = CategoryUtil.GetSafeCategoryId(ramp);

            foreach (IFCAnyHandle component in components)
                    {
                string componentName = IFCAnyHandleUtil.GetStringAttribute(component, "Name");
                string componentDescription = IFCAnyHandleUtil.GetStringAttribute(component, "Description");
                string componentObjectType = IFCAnyHandleUtil.GetStringAttribute(component, "ObjectType");
                string componentElementTag = IFCAnyHandleUtil.GetStringAttribute(component, "Tag");
                IFCAnyHandle componentProdRep = IFCAnyHandleUtil.GetInstanceAttribute(component, "Representation");

                IList<string> localComponentNames = new List<string>();
                IList<IFCAnyHandle> componentPlacementHnds = new List<IFCAnyHandle>();

                IFCAnyHandle localLocalPlacement = IFCAnyHandleUtil.GetObjectPlacement(component);
                IFCAnyHandle localRelativePlacement =
                    (localLocalPlacement == null) ? null : IFCAnyHandleUtil.GetInstanceAttribute(localLocalPlacement, "RelativePlacement");

                bool isSubRamp = component.IsSubTypeOf(IFCEntityType.IfcRamp.ToString());
                for (int ii = 0; ii < numFlights - 1; ii++)
                {
                    localComponentNames.Add((componentName == null) ? (ii + 2).ToString() : (componentName + ":" + (ii + 2)));
                    if (isSubRamp)
                        componentPlacementHnds.Add(ExporterUtil.CopyLocalPlacement(file, rampLocalPlacementHnds[ii]));
                    else
                        componentPlacementHnds.Add(IFCInstanceExporter.CreateLocalPlacement(file, rampLocalPlacementHnds[ii], localRelativePlacement));
                    }

                IList<IFCAnyHandle> localComponentHnds = new List<IFCAnyHandle>();
                if (isSubRamp)
                {
                    string componentType = IFCAnyHandleUtil.GetEnumerationAttribute(component, "ShapeType");
                    IFCRampType localRampType = GetIFCRampType(componentType);

                    for (int ii = 0; ii < numFlights - 1; ii++)
                    {
                        IFCAnyHandle representationCopy =
                            ExporterUtil.CopyProductDefinitionShape(exporterIFC, ramp, catId, componentProdRep);

                        localComponentHnds.Add(IFCInstanceExporter.CreateRamp(file, GUIDUtil.CreateGUID(), ownerHistory,
                            localComponentNames[ii], componentDescription, componentObjectType, componentPlacementHnds[ii], representationCopy,
                            componentElementTag, localRampType));
                    }
                }
                else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcRampFlight))
                {
                    for (int ii = 0; ii < numFlights - 1; ii++)
                    {
                        IFCAnyHandle representationCopy =
                            ExporterUtil.CopyProductDefinitionShape(exporterIFC, ramp, catId, componentProdRep);

                        localComponentHnds.Add(IFCInstanceExporter.CreateRampFlight(file, GUIDUtil.CreateGUID(), ownerHistory,
                            localComponentNames[ii], componentDescription, componentObjectType, componentPlacementHnds[ii], representationCopy,
                            componentElementTag));
                    }
                }
                else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcSlab))
                {
                    string componentType = IFCAnyHandleUtil.GetEnumerationAttribute(component, "PredefinedType");
                    IFCSlabType localLandingType = FloorExporter.GetIFCSlabType(componentType);

                    for (int ii = 0; ii < numFlights - 1; ii++)
                    {
                        IFCAnyHandle representationCopy =
                            ExporterUtil.CopyProductDefinitionShape(exporterIFC, ramp, catId, componentProdRep);

                        localComponentHnds.Add(IFCInstanceExporter.CreateSlab(file, GUIDUtil.CreateGUID(), ownerHistory,
                            localComponentNames[ii], componentDescription, componentObjectType, componentPlacementHnds[ii], representationCopy,
                            componentElementTag, localLandingType));
                    }
                }
                else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcMember))
                {
                    for (int ii = 0; ii < numFlights - 1; ii++)
                    {
                        IFCAnyHandle representationCopy =
                            ExporterUtil.CopyProductDefinitionShape(exporterIFC, ramp, catId, componentProdRep);

                        localComponentHnds.Add(IFCInstanceExporter.CreateMember(file, GUIDUtil.CreateGUID(), ownerHistory,
                            localComponentNames[ii], componentDescription, componentObjectType, componentPlacementHnds[ii], representationCopy,
                            componentElementTag));
                    }
                }

                for (int ii = 0; ii < numFlights - 1; ii++)
                {
                    if (localComponentHnds[ii] != null)
                    {
                        newComponents[ii].Add(localComponentHnds[ii]);
                        productWrapper.AddElement(localComponentHnds[ii], levelInfos[ii], componentECData[compIdx], false);
                    }
                }
                compIdx++;
            }

            // finally add a copy of the container.
            IList<IFCAnyHandle> rampCopyHnds = new List<IFCAnyHandle>();
            for (int ii = 0; ii < numFlights - 1; ii++)
            {
                string rampName = IFCAnyHandleUtil.GetStringAttribute(rampHnd, "Name");
                string rampObjectType = IFCAnyHandleUtil.GetStringAttribute(rampHnd, "ObjectType");
                string rampDescription = IFCAnyHandleUtil.GetStringAttribute(rampHnd, "Description");
                string rampElementTag = IFCAnyHandleUtil.GetStringAttribute(rampHnd, "Tag");
                string rampTypeAsString = IFCAnyHandleUtil.GetEnumerationAttribute(rampHnd, "ShapeType");
                IFCRampType rampType = GetIFCRampType(rampTypeAsString);

                string containerRampName = rampName + ":" + (ii + 2);
                rampCopyHnds.Add(IFCInstanceExporter.CreateRamp(file, GUIDUtil.CreateGUID(), ownerHistory,
                    containerRampName, rampDescription, rampObjectType, rampLocalPlacementHnds[ii], null, rampElementTag, rampType));

                productWrapper.AddElement(rampCopyHnds[ii], levelInfos[ii], null, true);
            }

            for (int ii = 0; ii < numFlights - 1; ii++)
            {
                StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(rampCopyHnds[ii], newComponents[ii],
                    rampLocalPlacementHnds[ii]);
                ExporterCacheManager.StairRampContainerInfoCache.AppendStairRampContainerInfo(ramp.Id, stairRampInfo);
            }
        }

        /// <summary>
        /// Exports a ramp to IfcRamp, without decomposing into separate runs and landings.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="ifcEnumType">The ramp type.</param>
        /// <param name="ramp">The ramp element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="numFlights">The number of flights for a multistory ramp.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void ExportRamp(ExporterIFC exporterIFC, string ifcEnumType, Element ramp, GeometryElement geometryElement,
            int numFlights, ProductWrapper productWrapper)
        {
            if (ramp == null || geometryElement == null)
                return;

            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                using (IFCPlacementSetter placementSetter = IFCPlacementSetter.Create(exporterIFC, ramp))
                {
                    using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
                    {
                        ecData.SetLocalPlacement(placementSetter.GetPlacement());
                        ecData.ReuseLocalPlacement = false;

                        GeometryElement rampGeom = GeometryUtil.GetOneLevelGeometryElement(geometryElement);

                        BodyData bodyData;
                        ElementId categoryId = CategoryUtil.GetSafeCategoryId(ramp);

                        BodyExporterOptions bodyExporterOptions = new BodyExporterOptions();
                        IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                            ramp, categoryId, rampGeom, bodyExporterOptions, null, ecData, out bodyData);

                        if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                        {
                            ecData.ClearOpenings();
                            return;
                        }

                        string containedRampGuid = ExporterIFCUtils.CreateSubElementGUID(ramp, (int)IFCRampSubElements.ContainedRamp);
                        IFCAnyHandle ownerHistory = exporterIFC.GetOwnerHistoryHandle();
                        string rampName = NamingUtil.GetNameOverride(ramp, NamingUtil.GetIFCName(ramp));
                        string rampDescription = NamingUtil.GetDescriptionOverride(ramp, null);
                        string rampObjectType = NamingUtil.GetObjectTypeOverride(ramp, NamingUtil.CreateIFCObjectName(exporterIFC, ramp));
                        IFCAnyHandle containedRampLocalPlacement = ExporterUtil.CreateLocalPlacement(file, ecData.GetLocalPlacement(), null);
                        string elementTag = NamingUtil.GetTagOverride(ramp, NamingUtil.CreateIFCElementId(ramp));
                        IFCRampType rampType = GetIFCRampType(ifcEnumType);

                        List<IFCAnyHandle> components = new List<IFCAnyHandle>();
                        IList<IFCExtrusionCreationData> componentExtrusionData = new List<IFCExtrusionCreationData>();
                        IFCAnyHandle containedRampHnd = IFCInstanceExporter.CreateRamp(file, containedRampGuid, ownerHistory, rampName,
                            rampDescription, rampObjectType, containedRampLocalPlacement, representation, elementTag, rampType);
                        components.Add(containedRampHnd);
                        componentExtrusionData.Add(ecData);
                        //productWrapper.AddElement(containedRampHnd, placementSetter.LevelInfo, ecData, false);
                        CategoryUtil.CreateMaterialAssociations(ramp.Document, exporterIFC, containedRampHnd, bodyData.MaterialIds);

                        string guid = GUIDUtil.CreateGUID(ramp);
                        IFCAnyHandle localPlacement = ecData.GetLocalPlacement();

                        IFCAnyHandle rampHnd = IFCInstanceExporter.CreateRamp(file, guid, ownerHistory, rampName,
                            rampDescription, rampObjectType, localPlacement, null, elementTag, rampType);

                        productWrapper.AddElement(rampHnd, placementSetter.GetLevelInfo(), ecData, true);

                        StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(rampHnd, components, localPlacement);
                        ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(ramp.Id, stairRampInfo);

                        ExportMultistoryRamp(exporterIFC, ramp, numFlights, rampHnd, components, componentExtrusionData, placementSetter, 
                            productWrapper);
                    }
                    PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, ramp, productWrapper);
                    tr.Commit();
                }
            }
        }
        
        /// <summary>
        /// Exports a ramp to IfcRamp.
        /// </summary>
        /// <param name="exporterIFC">The ExporterIFC object.</param>
        /// <param name="element">The ramp element.</param>
        /// <param name="geometryElement">The geometry element.</param>
        /// <param name="productWrapper">The ProductWrapper.</param>
        public static void Export(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, ProductWrapper productWrapper)
        {
            string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, element);
            IFCFile file = exporterIFC.GetFile();

            using (IFCTransaction tr = new IFCTransaction(file))
            {
                if (!(element is FamilyInstance))
                {
                    StairsExporter.ExportLegacyStairOrRampAsContainer(exporterIFC, ifcEnumType, element, geometryElement, productWrapper);
                    if (IFCAnyHandleUtil.IsNullOrHasNoValue(productWrapper.GetAnElement()))
                    {
                        int numFlights = GetNumFlightsForRamp(exporterIFC, element);
                        if (numFlights > 0)
                            ExportRamp(exporterIFC, ifcEnumType, element, geometryElement, numFlights, productWrapper);
                    }
                    else
                        PropertyUtil.CreateInternalRevitPropertySets(exporterIFC, element, productWrapper);
                }
                else
                {
                    ExportRamp(exporterIFC, ifcEnumType, element, geometryElement, 1, productWrapper);
                }

                tr.Commit();
            }
        }
    }
}
