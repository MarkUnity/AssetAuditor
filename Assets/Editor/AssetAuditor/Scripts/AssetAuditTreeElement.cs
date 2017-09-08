using System;

namespace UnityAssetAuditor
{
	[Serializable]
	public class AssetAuditTreeElement : TreeElement
    { 
		public bool isAsset;
	    public string projectPath;
	    public AssetAuditor.AssetType assetType;

	    internal enum AssetType
	    {
	        Asset,
            Directory
	    }

	    public bool conforms;

	    public AssetAuditTreeElement (string name,string _projectPath, int depth, int id, bool _isAsset, bool _conforms , AssetAuditor.AssetType _assetType) : base (name, depth, id)
		{
			isAsset = _isAsset;
		    projectPath = _projectPath;
	        conforms = _conforms;
			assetType = _assetType;
		}
	}
}
