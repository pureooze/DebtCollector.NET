using DebtCollector.NET;

namespace DebtCollector.Net.UnitTests;

public class PathGrouperTests {
    private readonly Dictionary<string, int> m_mostCommittedResult = new() {
        { "first/first/path/to/file1", 10 },
        { "first/first/path/to/file2", 2 },
        { "first/second/path/to/file3", 50 },
        { "first/second/path/to/file4", 4 },
        { "first/third/path/to/file5", 9 }
    };

    [Test]
    public void TryGetGroupedPaths_DepthLessThanZero_ReturnsFalse() {
        Dictionary<string, int> result = PathGrouper.GetGroupedPaths( 
            depth: -1, 
            mostCommittedResult: m_mostCommittedResult
        );
        
        Assert.That(result, Is.Empty);
    }
    
    [Test]
    public void TryGetGroupedPaths_DepthZero_ReturnsTrue() {
        Dictionary<string, int> result = PathGrouper.GetGroupedPaths( 
            depth: 0, 
            mostCommittedResult: m_mostCommittedResult
        );
 
        CollectionAssert.AreEquivalent( result, m_mostCommittedResult );
    }
    
    [Test]
    public void TryGetGroupedPaths_DepthOne_ReturnsTrue() {
        Dictionary<string, int> result = PathGrouper.GetGroupedPaths( 
            depth: 1, 
            mostCommittedResult: m_mostCommittedResult
        );
        
        Dictionary<string, int> expected = new() {
            { "first", 75 }
        };
            
        CollectionAssert.AreEquivalent( result, expected );
    }
    
    [Test]
    public void TryGetGroupedPaths_DepthTwo_ReturnsTrue() {
        Dictionary<string, int> result = PathGrouper.GetGroupedPaths( 
            depth: 2, 
            mostCommittedResult: m_mostCommittedResult
        );
        
        Dictionary<string, int> expected = new() {
            { "first/first", 12 },
            { "first/second", 54 },
            { "first/third", 9 }
        };
            
        CollectionAssert.AreEquivalent( result, expected );
    }
}