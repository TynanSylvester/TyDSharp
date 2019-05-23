using System;
using System.Collections.Generic;
using System.Linq;

namespace Tyd
{

    /// <summary>
    /// Handles inheritance between TydNodes via handle and source attributes.
    ///
    /// To use Inheritance:
    /// 1. Call Initialize().
    /// 2. Register all the nodes you want to interact with each other.
    /// 3. Call ResolveAll. This will modify the registered nodes in-place with any inheritance data.
    /// 4. Call Complete().
    ///
    /// It's recommended you use try/catch to ensure that Complete is always called.
    /// </summary>
    public static class Inheritance
    {
        private class InheritanceNode
        {
            public TydCollection tydNode;
            public bool resolved;
            public InheritanceNode source;        // Node from which I inherit.
            private List<InheritanceNode> heirs = null;  // Nodes which inherit from me.

            public InheritanceNode(TydCollection tydNode)
            {
                this.tydNode = tydNode;
            }

            public int HeirCount()
            {
                return heirs != null ? heirs.Count : 0;
            }

            public InheritanceNode GetHeir(int index)
            {
                return heirs[index];
            }

            public void AddHeir(InheritanceNode n)
            {
                if (heirs == null)
                    heirs = new List<InheritanceNode>();
                heirs.Add(n);
            }

            public override string ToString()
            {
                return tydNode.ToString();
            }
        }

        //Working vars
        private static bool initialized = false;
        private static List<InheritanceNode> nodesUnresolved = new List<InheritanceNode>();
        private static Dictionary<TydNode, InheritanceNode> nodesResolved = new Dictionary<TydNode, InheritanceNode>();
        private static Dictionary<string, InheritanceNode> nodesByHandle = new Dictionary<string, InheritanceNode>();


        public static void Initialize()
        {
            if (initialized)
                throw new Exception("Initialized Tyd.Inheritance when it was already initialized. Call Complete() first.");

            initialized = true;
        }

        public static void Complete()
        {
            //We clear these first because even if it's not initialized, in case of something going really wrong we still want these to end up cleared.
            nodesResolved.Clear();
            nodesUnresolved.Clear();
            nodesByHandle.Clear();

            if (!initialized)
                throw new Exception("Completed Tyd.Inheritance when it was not initialized. Call Initialize() first.");

            initialized = false;
        }

        ///<summary>
        /// Registers a single node.
        /// When we resolve later, we'll be able to use this node as a source.
        ///</summary>
        public static void Register(TydCollection node)
        {
            if (!initialized)
                throw new Exception("Used Tyd.Inheritance when it was not initialized.");

            //If the node has no handle, and no source, we can ignore it since it's not connected to inheritance at all.
            var nodeHandle = node.AttributeHandle;
            var nodeSource = node.AttributeSource;
            if (nodeHandle == null && nodeSource == null)
                return;

            //Ensure we're don't have two nodes of the same handle
            if (nodeHandle != null && nodesByHandle.ContainsKey(nodeHandle))
                throw new Exception("Tyd error: Multiple Tyd nodes with the same handle " + nodeHandle + ".");

            //Make an inheritance node for the Tyd node
            var newNode = new InheritanceNode(node);
            nodesUnresolved.Add(newNode);
            if (nodeHandle != null)
                nodesByHandle.Add(nodeHandle, newNode);
        }

        ///<summary>
        /// Registers all nodes from doc.
        /// When we resolve later, we'll be able to use the nodes in this document as a sources.
        ///</summary>
        public static void RegisterAllFrom(TydDocument doc)
        {
            if (!initialized)
                throw new Exception("Used Tyd.Inheritance when it was not initialized.");

            for (int i = 0; i < doc.Count; i++)
            {
                var tydCol = doc[i] as TydCollection;

                if (tydCol != null)
                    Register(tydCol);
            }
        }

        ///<summary>
        /// Resolves all registered nodes.
        ///</summary>
        public static void ResolveAll()
        {
            if (!initialized)
                throw new Exception("Used Tyd.Inheritance when it was not initialized.");

            LinkAllInheritanceNodes();
            ResolveAllUnresolvedInheritanceNodes();
        }

        /// <summary>
        /// Link all unresolved nodes to their sources and heirs.
        /// </summary>
        private static void LinkAllInheritanceNodes()
        {
            for (int i = 0; i < nodesUnresolved.Count; i++)
            {
                var urn = nodesUnresolved[i];

                var attSource = urn.tydNode.AttributeSource;
                if (attSource == null)
                    continue;

                if (!nodesByHandle.TryGetValue(attSource, out urn.source))
                    throw new Exception("Could not find source node named '" + attSource + "' for Tyd node: " + urn.tydNode.FullTyd);

                if (urn.source != null)
                    urn.source.AddHeir(urn);
            }
        }

        /// <summary>
        /// Merge all unresolved nodes with their source nodes.
        /// </summary>
        private static void ResolveAllUnresolvedInheritanceNodes()
        {
            // find roots from which we'll start resolving nodes,
            // a node is a root node if it has null source or its source has been already resolved,
            // this method works only for single inheritance!
            var roots = nodesUnresolved.Where(x => x.source == null || x.source.resolved).ToList(); // important to make a copy

            for (int i = 0; i < roots.Count; i++)
            {
                ResolveInheritanceNodeAndHeirs(roots[i]);
            }

            // check if there are any unresolved nodes (if there are, then it means that there is a cycle),
            // and move nodes to resolved nodes collection
            for (int i = 0; i < nodesUnresolved.Count; i++)
            {
                if (!nodesUnresolved[i].resolved)
                {
                    throw new FormatException("Tyd error: Cyclic inheritance detected for node:\n" + nodesUnresolved[i].tydNode.FullTyd);
                    //continue;
                }
                nodesResolved.Add(nodesUnresolved[i].tydNode, nodesUnresolved[i]);
            }

            nodesUnresolved.Clear();
        }

        ///<summary>
        /// Resolves given node and then all its heir nodes recursively using DFS.
        ///</summary>
        private static void ResolveInheritanceNodeAndHeirs(InheritanceNode node)
        {
            //Error check
            // if we've reached a resolved node by traversing the tree, then it means
            // that there's a cycle, note that we're not reporting the full cycle in
            // the error message here, but only the last node which created a cycle
            if (node.resolved)
                throw new Exception("Cyclic inheritance detected for Tyd node:\n" + node.tydNode.FullTyd);

            //Resolve this node
            {
                if (node.source == null)
                {
                    // No source - Just use the original node
                    node.resolved = true;
                }
                else
                {
                    //Source exists - We now inherit from it
                    //We must use source's RESOLVED node here because our source can have its own source.
                    if (!node.source.resolved)
                        throw new Exception("Tried to resolve Tyd inheritance node " + node + " whose source has not been resolved yet. This means that this method was called in incorrect order.");

                    CheckForDuplicateNodes(node.tydNode);

                    node.resolved = true;

                    //Apply inheritance from source to node
                    ApplyInheritance(node.source.tydNode, node.tydNode);
                }
            }

            //Recur to the heirs and resolve them too
            for (int i = 0; i < node.HeirCount(); i++)
            {
                ResolveInheritanceNodeAndHeirs(node.GetHeir(i));
            }
        }

        ///<summary>
        /// Copies all child nodes from source into heir, recursively.
        /// -If a node appears only in source or only in heir, it is included.
        /// -If a list appears in both source and heir, source's entries are appended to heir's entries.
        /// -If a non-list node appears in both source and heir, heir's node is overwritten.
        ///</summary>
        private static void ApplyInheritance(TydNode source, TydNode heir)
        {
            try
            {
                //They're either strings or nulls: We just keep the existing heir's value
                if (source is TydString)
                    return;

                //Heir has noinherit attribute: Skip this inheritance
                {
                    TydCollection heirCol = heir as TydCollection;
                    if( heirCol != null && heirCol.AttributeNoInherit )
                        return;
                }

                //They're tables: Combine all children of source and heir. Unique-name source nodes are prepended
                {
                    TydTable sourceObj = source as TydTable;
                    if (sourceObj != null)
                    {
                        TydTable heirTable = (TydTable)heir;
                        for (int i = 0; i < sourceObj.Count; i++)
                        {
                            var sourceChild = sourceObj[i];
                            var heirMatchingChild = heirTable[sourceChild.Name];

                            if (heirMatchingChild != null)
                                ApplyInheritance(sourceChild, heirMatchingChild);
                            else
                                heirTable.InsertChild(sourceChild, 0); //Does this need to be DeepClone?
                        }
                        return;
                    }
                }

                //They're lists: Prepend source's children before heir's children
                {
                    TydList sourceList = source as TydList;
                    if (sourceList != null)
                    {
                        TydList heirList = (TydList)heir;
                        for (int i = 0; i < sourceList.Count; i++)
                        {
                            heirList.InsertChild(sourceList[i], 0); //Does this need to be DeepClone?
                        }
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("ApplyInheritance exception: " + e + ".\nsource: (" + source + ")\n" + TydToText.Write(source) + "\ntarget: (" + heir + ")\n" + TydToText.Write(heir));
            }
        }

        private static HashSet<string> tempUsedNodeNames = new HashSet<string>();
        private static void CheckForDuplicateNodes(TydCollection originalNode)
        {
            //This is needed despite another check elsewhere
            //Because the source-data-combination process wipes out duplicate Tyd data

            tempUsedNodeNames.Clear();

            for (int i = 0; i < originalNode.Count; i++)
            {
                var node = originalNode[i];

                if (node.Name == null)
                    continue;

                if (tempUsedNodeNames.Contains(node.Name))
                    throw new FormatException("Tyd error: Duplicate Tyd node name " + node.Name + " in this Tyd block: " + originalNode);
                else
                    tempUsedNodeNames.Add(node.Name);
            }

            tempUsedNodeNames.Clear();
        }
    }

}