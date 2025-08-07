/// <summary>
/// Entry point for the Content Builder project, 
/// which when executed will build content according to the "Content Collection Strategy" defined in the MyContentCollector class.
/// </summary>
/// <remarks>
/// Make sure to validate the directory paths in the "ContentBuilderParams" for your specific project.
/// For more details regarding the Content Builder, see the MonoGame documentation: <tbc.>
/// </remarks>
///

using PlatformerContentBuilder;
using MonoGame.Framework.Content.Pipeline.Builder;


var contentCollectionArgs = ContentBuilderParams.Parse(args);
if (contentCollectionArgs == null)
    return -1;

var contentCollector = new MyContentCollector();
contentCollector.Run(contentCollectionArgs);

return 0;
