using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityAssetAuditor
{
	public class AssetAuditorUtilities
	{
		public class CreatePath : IEnumerable
		{
			private string[] m_Folders;

			public CreatePath( string path )
			{
				Assert.IsTrue( path.StartsWith( "Assets/" ) );
				m_Folders = path.Split( '/' );
			}

			public CreatePath( string[] folderList )
			{
				Assert.IsTrue( folderList.Length > 1 && folderList[0] == "Assets" );

				m_Folders = new string[folderList.Length];
				for( int i = 0; i < folderList.Length; i++ )
					m_Folders[i] = folderList[i];
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public CreatePathEnumerator GetEnumerator()
			{
				return new CreatePathEnumerator( m_Folders );
			}

			public static string Create( string path )
			{
				CreatePathEnumerator e = new CreatePathEnumerator( path );
				while( e.MoveNext() )
				{
					// Loop through each folder
				}

				return e.Current;
			}
		}

		public class CreatePathEnumerator : IEnumerator
		{
			private string[] m_FolderNames;
			private int m_Position = 0;

			private string m_CurrentGuid;
			private string m_CurrentPath;

			public CreatePathEnumerator( string[] folderList )
			{
				m_FolderNames = folderList;
				m_CurrentPath = m_FolderNames[0];
			}

			public CreatePathEnumerator( string path )
			{
				Debug.Log( path );
				m_FolderNames = path.Split( '/' );
				m_CurrentPath = m_FolderNames[0];
			}

			public bool MoveNext()
			{
				m_Position++;

				if( m_Position < m_FolderNames.Length )
				{
					string nextPath = m_CurrentPath + "/" + m_FolderNames[m_Position];
					if( AssetDatabase.IsValidFolder( nextPath ) )
						m_CurrentGuid = string.Empty;
					else
						m_CurrentGuid = AssetDatabase.CreateFolder( m_CurrentPath, m_FolderNames[m_Position] );

					m_CurrentPath = nextPath;
					return true;
				}

				return false;
			}

			public void Reset()
			{
				m_Position = 0;
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}

			public string Current
			{
				get { return m_CurrentGuid; }
			}
		}
	}
}