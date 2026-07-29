# Repository Agent Instructions

## Memory and performance improvements

When improving memory usage or performance, prefer allocation-aware APIs where they are practical and make the code measurably or clearly more efficient:

- Prefer `stackalloc` for small, bounded, short-lived buffers whose spans do not escape the current synchronous scope. Never derive an unbounded stack allocation size directly from user input or variable-size network data.
- Prefer DotNext's `SpanOwner<T>` for temporary span-based storage and `MemoryOwner<T>` when owned memory must cross method, async, or component boundaries. Dispose owners deterministically and do not use their spans or memory after disposal.
- Use DotNext dynamic buffer writers for growable data instead of repeatedly reallocating arrays.
- Prefer buffer writers over `StringBuilder` when constructing data that can be written incrementally to spans or buffers without requiring string-specific formatting behavior.
- Prefer `BufferWriterSlim<T>` whenever the operation is synchronous and a sensible initial buffer can be allocated on the stack. It must not be used across `await` boundaries.
- Do not add pooling or ownership abstractions when they increase complexity without removing meaningful allocations. Validate performance-sensitive changes with tests, benchmarks, or profiling when practical.

Choose a growable buffer using this guidance:

| Buffer writer | When to use | Async-compatible | Write space complexity |
| --- | --- | --- | --- |
| `PoolingArrayBufferWriter<T>` | General-purpose use when the initial capacity is known. | Yes | O(1) amortized, O(n) when growing |
| `PoolingBufferWriter<T>` | A custom memory allocator is required, such as an unmanaged memory pool. | Yes | O(1) amortized, O(n) when growing |
| `BufferWriterSlim<T>` | An effective initial size is known and can be allocated on the stack, avoiding an initial pool rent and managed heap allocation. Prefer this whenever possible. | No | O(1) amortized, O(n) when growing |
| `SparseBufferWriter<T>` | The optimal initial size is unknown and written lengths vary widely. It provides constant-space writes by storing sparse chunks. | Yes | O(1) |

Prefer `SparseBufferWriter<T>` over `RecyclableMemoryStream` for large or highly variable buffers when its generic element support and sparse growth model fit the operation.
