using NUnit.Framework;

namespace OmniSharp.Tests.AutoComplete
{
	[TestFixture]
	public class SnippetTests : CompletionTestBase
	{
		[Test]
		public void Should_template_generic_type_argument()
		{
			SnippetFor(
                @"using System.Collections.Generic;
            public class Class1 {
                public Class1()
                {
                    var l = new Lis$
                }
            }")
                .ShouldContain("List<${1:T}>()$0");	   
		}

		[Test]
		public void Should_template_generic_type_arguments()
		{
			SnippetFor(
                @"using System.Collections.Generic;
            public class Class1 {
                public Class1()
                {
                    var l = new Dict$
                }
            }")
                .ShouldContain("Dictionary<${1:TKey}, ${2:TValue}>()$0");	   
		}

		[Test]
		public void Should_template_parameter()
		{
			SnippetFor(
                @"using System.Collections.Generic;
            public class Class1 {
                public Class1()
                {
                    var l = new Lis$
                }
            }")
                .ShouldContain("List<${1:T}>(${2:IEnumerable<T> collection})$0");	   
		}
	}
    
}