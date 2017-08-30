# Asset Auditor

The asset auditor tool is designed to allow you to create sets of rules for the import settings for your assets in a Unity project and fix any assets that do not comply to those rules. 

#### The new audit rule window. 
This window allows you to create a new rule for you assets. 

**Rule Name** - The first step is to give the rule a name. This can be whatever you desire it to be and will be name given to the proxy/dummy asset stored in the project. The reason for using a proxy/dummy assets is to provide a full inspector for the asset so that you modify the settings properties in a familiar manner to modify any other asset in
your project. 

**Wild Card Matching Type** - The wild card matching type has two options. 

**Name Contains** - Name contains works by matching the providing string to any of the asset names in the project that are of the same asset type as the asset rule. So If you set the name contains string to "nrm" on texture based rule.  Any texture asset that has nrm in the name will be found by the rule when inspecting in audit view.

**Regex** - This work the same as name contains but uses full regex pattern matching. N.B. This is C# regex so there is no need for start and end /

**Wild Card** - This is the string that is used in the Wild Card Matching Type for finding assets that the rule should effect. 

**Selective Mode** - This lets you create a rule that will only overwrite certain settings from a drop down The drop down enabling selective mode creates allows you to add as many selective element as you like and selectwhich rules you would like to override from it. 

**Rule Type** - This determines what assets the rule should affect. Currently there are three supported asset types. Texture, for all your imported textures, Model, for all your imported models and Audio for the imported audio files. 


During the creation of the rule new audit rule window will display a list of the assets that will be found by that rule so you can preview your results.

#### Audit View
The audit view allows you to see the assets that the rule wild card will cover and apply the desired import settings to if desired. It also allows for modification of the wildcard property in case of naming convention changes. 

**Rule** - this is a drop down list of the available rules in the project. 

**Wild Card** - This can be used to modify the wild card that has been supplied to original rule. 

**Selective** - A drop down showing the properties that will be overriden. 

**Search bar** - This allows for searching of the the assets that are in the tree view. 

**Tree view** - This shows the subset of the project view that contains the assets that the rule has found through its search based on the wild card and wild card matching type.

**Tool Bar** - This contains three options

**Expand all** - This expands every element in the tree view

**Collapse all** - This collapses the entire tree view

**Fix all**- This fixes every asset that has been found to not match rules import settings. 



## In Development
Improvements to selective mode.
UI improvements. 
Folder level overrides. 

