﻿/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;
using MongoDB.Driver.Search;

namespace MongoDB.Driver
{
    /// <summary>
    /// Methods for building pipeline stages.
    /// </summary>
    public static class PipelineStageDefinitionBuilder
    {
        /// <summary>
        /// Creates a $bucket stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="groupBy">The group by expression.</param>
        /// <param name="boundaries">The boundaries.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, AggregateBucketResult<TValue>> Bucket<TInput, TValue>(
            AggregateExpressionDefinition<TInput, TValue> groupBy,
            IEnumerable<TValue> boundaries,
            AggregateBucketOptions<TValue> options = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsNotNull(boundaries, nameof(boundaries));

            const string operatorName = "$bucket";
            var stage = new DelegatedPipelineStageDefinition<TInput, AggregateBucketResult<TValue>>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var valueSerializer = sr.GetSerializer<TValue>();
                    var renderedGroupBy = groupBy.Render(s, sr, linqProvider);
                    var serializedBoundaries = boundaries.Select(b => valueSerializer.ToBsonValue(b));
                    var serializedDefaultBucket = options != null && options.DefaultBucket.HasValue ? valueSerializer.ToBsonValue(options.DefaultBucket.Value) : null;
                    var document = new BsonDocument
                    {
                        { operatorName, new BsonDocument
                            {
                                { "groupBy", renderedGroupBy },
                                { "boundaries", new BsonArray(serializedBoundaries) },
                                { "default", serializedDefaultBucket, serializedDefaultBucket != null }
                            }
                        }
                    };
                    return new RenderedPipelineStageDefinition<AggregateBucketResult<TValue>>(
                        operatorName,
                        document,
                        sr.GetSerializer<AggregateBucketResult<TValue>>());
                });

            return stage;
        }

        /// <summary>
        /// Creates a $bucket stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="groupBy">The group by expression.</param>
        /// <param name="boundaries">The boundaries.</param>
        /// <param name="output">The output projection.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Bucket<TInput, TValue, TOutput>(
            AggregateExpressionDefinition<TInput, TValue> groupBy,
            IEnumerable<TValue> boundaries,
            ProjectionDefinition<TInput, TOutput> output,
            AggregateBucketOptions<TValue> options = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsNotNull(boundaries, nameof(boundaries));
            Ensure.IsNotNull(output, nameof(output));

            const string operatorName = "$bucket";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var valueSerializer = sr.GetSerializer<TValue>();
                    var outputSerializer = sr.GetSerializer<TOutput>();
                    var renderedGroupBy = groupBy.Render(s, sr, linqProvider);
                    var serializedBoundaries = boundaries.Select(b => valueSerializer.ToBsonValue(b));
                    var serializedDefaultBucket = options != null && options.DefaultBucket.HasValue ? valueSerializer.ToBsonValue(options.DefaultBucket.Value) : null;
                    var renderedOutput = output.Render(s, sr, linqProvider);
                    var document = new BsonDocument
                    {
                        { operatorName, new BsonDocument
                            {
                                { "groupBy", renderedGroupBy },
                                { "boundaries", new BsonArray(serializedBoundaries) },
                                { "default", serializedDefaultBucket, serializedDefaultBucket != null },
                                { "output", renderedOutput.Document }
                            }
                        }
                    };
                    return new RenderedPipelineStageDefinition<TOutput>(
                        operatorName,
                        document,
                        outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $bucket stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="groupBy">The group by expression.</param>
        /// <param name="boundaries">The boundaries.</param>
        /// <param name="options">The options.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, AggregateBucketResult<TValue>> Bucket<TInput, TValue>(
            Expression<Func<TInput, TValue>> groupBy,
            IEnumerable<TValue> boundaries,
            AggregateBucketOptions<TValue> options = null,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            return Bucket(
                new ExpressionAggregateExpressionDefinition<TInput, TValue>(groupBy, translationOptions),
                boundaries,
                options);
        }

        /// <summary>
        /// Creates a $bucket stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="groupBy">The group by expression.</param>
        /// <param name="boundaries">The boundaries.</param>
        /// <param name="output">The output projection.</param>
        /// <param name="options">The options.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Bucket<TInput, TValue, TOutput>(
            Expression<Func<TInput, TValue>> groupBy,
            IEnumerable<TValue> boundaries,
            Expression<Func<IGrouping<TValue, TInput>, TOutput>> output,
            AggregateBucketOptions<TValue> options = null,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsNotNull(output, nameof(output));
            return new BucketWithOutputExpressionStageDefinition<TInput, TValue, TOutput>(groupBy, boundaries, output, options, translationOptions);
        }

        /// <summary>
        /// Creates a $bucketAuto stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="groupBy">The group by expression.</param>
        /// <param name="buckets">The number of buckets.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, AggregateBucketAutoResult<TValue>> BucketAuto<TInput, TValue>(
            AggregateExpressionDefinition<TInput, TValue> groupBy,
            int buckets,
            AggregateBucketAutoOptions options = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsGreaterThanZero(buckets, nameof(buckets));

            const string operatorName = "$bucketAuto";
            var stage = new DelegatedPipelineStageDefinition<TInput, AggregateBucketAutoResult<TValue>>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var renderedGroupBy = groupBy.Render(s, sr, linqProvider);
                    var document = new BsonDocument
                    {
                            { operatorName, new BsonDocument
                                {
                                    { "groupBy", renderedGroupBy },
                                    { "buckets", buckets },
                                    { "granularity", () => options.Granularity.Value.Value, options != null && options.Granularity.HasValue }
                                }
                            }
                    };
                    return new RenderedPipelineStageDefinition<AggregateBucketAutoResult<TValue>>(
                        operatorName,
                        document,
                        sr.GetSerializer<AggregateBucketAutoResult<TValue>>());
                });

            return stage;
        }

        /// <summary>
        /// Creates a $bucketAuto stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="groupBy">The group by expression.</param>
        /// <param name="buckets">The number of buckets.</param>
        /// <param name="output">The output projection.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> BucketAuto<TInput, TValue, TOutput>(
            AggregateExpressionDefinition<TInput, TValue> groupBy,
            int buckets,
            ProjectionDefinition<TInput, TOutput> output,
            AggregateBucketAutoOptions options = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsGreaterThanZero(buckets, nameof(buckets));
            Ensure.IsNotNull(output, nameof(output));

            const string operatorName = "$bucketAuto";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var outputSerializer = sr.GetSerializer<TOutput>();
                    var renderedGroupBy = groupBy.Render(s, sr, linqProvider);
                    var renderedOutput = output.Render(s, sr, linqProvider);
                    var document = new BsonDocument
                    {
                        { operatorName, new BsonDocument
                            {
                                { "groupBy", renderedGroupBy },
                                { "buckets", buckets },
                                { "output", renderedOutput.Document },
                                { "granularity", () => options.Granularity.Value.Value, options != null && options.Granularity.HasValue }
                           }
                        }
                    };
                    return new RenderedPipelineStageDefinition<TOutput>(
                        operatorName,
                        document,
                        outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $bucketAuto stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="groupBy">The group by expression.</param>
        /// <param name="buckets">The number of buckets.</param>
        /// <param name="options">The options (optional).</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, AggregateBucketAutoResult<TValue>> BucketAuto<TInput, TValue>(
            Expression<Func<TInput, TValue>> groupBy,
            int buckets,
            AggregateBucketAutoOptions options = null,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            return BucketAuto(
                new ExpressionAggregateExpressionDefinition<TInput, TValue>(groupBy, translationOptions),
                buckets,
                options);
        }

        /// <summary>
        /// Creates a $bucketAuto stage (this overload can only be used with LINQ3).
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the output documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="groupBy">The group by expression.</param>
        /// <param name="buckets">The number of buckets.</param>
        /// <param name="output">The output projection.</param>
        /// <param name="options">The options (optional).</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> BucketAuto<TInput, TValue, TOutput>(
            Expression<Func<TInput, TValue>> groupBy,
            int buckets,
            Expression<Func<IGrouping<AggregateBucketAutoResultId<TValue>, TInput>, TOutput>> output,
            AggregateBucketAutoOptions options = null,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsNotNull(output, nameof(output));
            return new BucketAutoWithOutputExpressionStageDefinition<TInput, TValue, TOutput>(groupBy, buckets, output, options);
        }

        /// <summary>
        /// Creates a $bucketAuto stage (this method can only be used with LINQ2).
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the output documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="groupBy">The group by expression.</param>
        /// <param name="buckets">The number of buckets.</param>
        /// <param name="output">The output projection.</param>
        /// <param name="options">The options (optional).</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> BucketAutoForLinq2<TInput, TValue, TOutput>(
            Expression<Func<TInput, TValue>> groupBy,
            int buckets,
            Expression<Func<IGrouping<TValue, TInput>, TOutput>> output, // the IGrouping for BucketAuto has been wrong all along, only fixing it for LINQ3
            AggregateBucketAutoOptions options = null,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsNotNull(output, nameof(output));
            return BucketAuto(
                new ExpressionAggregateExpressionDefinition<TInput, TValue>(groupBy, translationOptions),
                buckets,
                new ExpressionBucketOutputProjection<TInput, TValue, TOutput>(x => default(TValue), output, translationOptions),
                options);
        }

        /// <summary>
        /// Creates a $changeStream stage.
        /// Normally you would prefer to use the Watch method of <see cref="IMongoCollection{TDocument}" />.
        /// Only use this method if subsequent stages project away the resume token (the _id)
        /// or you don't want the resulting cursor to automatically resume.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, ChangeStreamDocument<TInput>> ChangeStream<TInput>(
            ChangeStreamStageOptions options = null)
        {
            options = options ?? new ChangeStreamStageOptions();

            const string operatorName = "$changeStream";
            var stage = new DelegatedPipelineStageDefinition<TInput, ChangeStreamDocument<TInput>>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var renderedOptions = new BsonDocument
                    {
                        { "fullDocument", () => MongoUtils.ToCamelCase(options.FullDocument.ToString()), options.FullDocument != ChangeStreamFullDocumentOption.Default },
                        { "allChangesForCluster", true, options.AllChangesForCluster ?? false },
                        { "resumeAfter", options.ResumeAfter, options.ResumeAfter != null },
                        { "startAfter", options.StartAfter, options.StartAfter != null },
                        { "startAtOperationTime", options.StartAtOperationTime, options.StartAtOperationTime != null }
                    };
                    var document = new BsonDocument(operatorName, renderedOptions);
                    var outputSerializer = new ChangeStreamDocumentSerializer<TInput>(s);
                    return new RenderedPipelineStageDefinition<ChangeStreamDocument<TInput>>(
                        operatorName,
                        document,
                        outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $changeStreamSplitLargeEvent stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<ChangeStreamDocument<TInput>, ChangeStreamDocument<TInput>> ChangeStreamSplitLargeEvent<TInput>() =>
            (PipelineStageDefinition<ChangeStreamDocument<TInput>, ChangeStreamDocument<TInput>>)new BsonDocument("$changeStreamSplitLargeEvent", new BsonDocument());

        /// <summary>
        /// Creates a $count stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, AggregateCountResult> Count<TInput>()
        {
            const string operatorName = "$count";
            var stage = new DelegatedPipelineStageDefinition<TInput, AggregateCountResult>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    return new RenderedPipelineStageDefinition<AggregateCountResult>(
                        operatorName,
                        new BsonDocument(operatorName, "count"),
                        sr.GetSerializer<AggregateCountResult>());
                });

            return stage;
        }

        /// <summary>
        /// Creates a $densify stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="range">The range.</param>
        /// <param name="partitionByFields">The partition by fields.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Densify<TInput>(
            FieldDefinition<TInput> field,
            DensifyRange range, // can be null
            IEnumerable<FieldDefinition<TInput>> partitionByFields = null)
        {
            Ensure.IsNotNull(field, nameof(field));
            Ensure.IsNotNull(range, nameof(range));

            const string operatorName = "$densify";
            var stage = new DelegatedPipelineStageDefinition<TInput, TInput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var renderedPartitionByFields = partitionByFields?.Select(f => f.Render(s, sr, linqProvider).FieldName).ToList();
                    var document = new BsonDocument
                    {
                        {
                            operatorName, new BsonDocument
                            {
                                { "field", field.Render(s, sr, linqProvider).FieldName },
                                { "partitionByFields", () => new BsonArray(renderedPartitionByFields), partitionByFields != null && renderedPartitionByFields.Count > 0 },
                                { "range", range.Render() }
                            }
                        }
                    };
                    return new RenderedPipelineStageDefinition<TInput>(operatorName, document, s);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $densify stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="range">The range.</param>
        /// <param name="partitionByFields">The partition by fields.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Densify<TInput>(
            FieldDefinition<TInput> field,
            DensifyRange range, // can be null
            params FieldDefinition<TInput>[] partitionByFields)
        {
            Ensure.IsNotNull(field, nameof(field));
            Ensure.IsNotNull(range, nameof(range));
            return Densify(field, range, (IEnumerable<FieldDefinition<TInput>>)partitionByFields);
        }

        /// <summary>
        /// Creates a $densify stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="range">The range.</param>
        /// <param name="partitionByFields">The partition by fields.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Densify<TInput>(
            Expression<Func<TInput, object>> field,
            DensifyRange range, // can be null
            IEnumerable<Expression<Func<TInput, object>>> partitionByFields = null)
        {
            Ensure.IsNotNull(field, nameof(field));
            Ensure.IsNotNull(range, nameof(range));
            return Densify(
                new ExpressionFieldDefinition<TInput>(field),
                range,
                partitionByFields?.Select(f => new ExpressionFieldDefinition<TInput>(f)));
        }

        /// <summary>
        /// Creates a $densify stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="range">The range.</param>
        /// <param name="partitionByFields">The partition by fields.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Densify<TInput>(
            Expression<Func<TInput, object>> field,
            DensifyRange range, // can be null
            params Expression<Func<TInput, object>>[] partitionByFields)
        {
            Ensure.IsNotNull(field, nameof(field));
            Ensure.IsNotNull(range, nameof(range));
            return Densify(field, range, (IEnumerable<Expression<Func<TInput, object>>>)partitionByFields);
        }

        /// <summary>
        /// Creates a $documents stage.
        /// </summary>
        /// <typeparam name="TDocument">The type of the documents.</typeparam>
        /// <param name="documents">The documents.</param>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<NoPipelineInput, TDocument> Documents<TDocument>(
            AggregateExpressionDefinition<NoPipelineInput, IEnumerable<TDocument>> documents,
            IBsonSerializer<TDocument> documentSerializer = null)
        {
            if (typeof(TDocument) == typeof(NoPipelineInput))
            {
                throw new ArgumentException("Documents cannot be of type NoPipelineInput.", nameof(documents));
            }

            const string operatorName = "$documents";
            var stage = new DelegatedPipelineStageDefinition<NoPipelineInput, TDocument>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var renderedDocuments = documents.Render(NoPipelineInputSerializer.Instance, sr, linqProvider);
                    return new RenderedPipelineStageDefinition<TDocument>(
                        operatorName,
                        new BsonDocument(operatorName, renderedDocuments),
                        documentSerializer ?? sr.GetSerializer<TDocument>());
                });

            return stage;
        }

        /// <summary>
        /// Creates a $documents stage.
        /// </summary>
        /// <typeparam name="TDocument">The type of the documents.</typeparam>
        /// <param name="documents">The documents.</param>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<NoPipelineInput, TDocument> Documents<TDocument>(
            IEnumerable<TDocument> documents,
            IBsonSerializer<TDocument> documentSerializer = null)
        {
            var aggregateExpression = new DocumentsAggregateExpressionDefinition<TDocument>(documents, documentSerializer);
            return Documents(aggregateExpression, documentSerializer);
        }

        /// <summary>
        /// Creates a $facet stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="facets">The facets.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Facet<TInput, TOutput>(
            IEnumerable<AggregateFacet<TInput>> facets,
            AggregateFacetOptions<TOutput> options = null)
        {
            Ensure.IsNotNull(facets, nameof(facets));

            const string operatorName = "$facet";
            var materializedFacets = facets.ToArray();
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var facetsDocument = new BsonDocument();
                    foreach (var facet in materializedFacets)
                    {
                        var renderedPipeline = facet.RenderPipeline(s, sr, linqProvider);
                        facetsDocument.Add(facet.Name, renderedPipeline);
                    }
                    var document = new BsonDocument("$facet", facetsDocument);
                    var outputSerializer = options?.OutputSerializer ?? sr.GetSerializer<TOutput>();
                    return new RenderedPipelineStageDefinition<TOutput>(operatorName, document, outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $facet stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="facets">The facets.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, AggregateFacetResults> Facet<TInput>(
            IEnumerable<AggregateFacet<TInput>> facets)
        {
            Ensure.IsNotNull(facets, nameof(facets));
            var outputSerializer = new AggregateFacetResultsSerializer(
                facets.Select(f => f.Name),
                facets.Select(f => f.OutputSerializer ?? BsonSerializer.SerializerRegistry.GetSerializer(f.OutputType)));
            var options = new AggregateFacetOptions<AggregateFacetResults> { OutputSerializer = outputSerializer };
            return Facet(facets, options);
        }

        /// <summary>
        /// Creates a $facet stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="facets">The facets.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, AggregateFacetResults> Facet<TInput>(
            params AggregateFacet<TInput>[] facets)
        {
            return Facet((IEnumerable<AggregateFacet<TInput>>)facets);
        }

        /// <summary>
        /// Creates a $facet stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="facets">The facets.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Facet<TInput, TOutput>(
            params AggregateFacet<TInput>[] facets)
        {
            return Facet<TInput, TOutput>((IEnumerable<AggregateFacet<TInput>>)facets);
        }

        /// <summary>
        /// Creates a $graphLookup stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TFrom">The type of the from documents.</typeparam>
        /// <typeparam name="TConnectFrom">The type of the connect from field (must be either TConnectTo or a type that implements IEnumerable{TConnectTo}).</typeparam>
        /// <typeparam name="TConnectTo">The type of the connect to field.</typeparam>
        /// <typeparam name="TStartWith">The type of the start with expression (must be either TConnectTo or a type that implements IEnumerable{TConnectTo}).</typeparam>
        /// <typeparam name="TAsElement">The type of the as field elements.</typeparam>
        /// <typeparam name="TAs">The type of the as field.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="from">The from collection.</param>
        /// <param name="connectFromField">The connect from field.</param>
        /// <param name="connectToField">The connect to field.</param>
        /// <param name="startWith">The start with value.</param>
        /// <param name="as">The as field.</param>
        /// <param name="depthField">The depth field.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> GraphLookup<TInput, TFrom, TConnectFrom, TConnectTo, TStartWith, TAsElement, TAs, TOutput>(
            IMongoCollection<TFrom> from,
            FieldDefinition<TFrom, TConnectFrom> connectFromField,
            FieldDefinition<TFrom, TConnectTo> connectToField,
            AggregateExpressionDefinition<TInput, TStartWith> startWith,
            FieldDefinition<TOutput, TAs> @as,
            FieldDefinition<TAsElement, int> depthField,
            AggregateGraphLookupOptions<TFrom, TAsElement, TOutput> options = null)
                where TAs : IEnumerable<TAsElement>
        {
            Ensure.IsNotNull(from, nameof(from));
            Ensure.IsNotNull(connectFromField, nameof(connectFromField));
            Ensure.IsNotNull(connectToField, nameof(connectToField));
            Ensure.IsNotNull(startWith, nameof(startWith));
            Ensure.IsNotNull(@as, nameof(@as));
            Ensure.That(AreGraphLookupFromAndToTypesCompatible<TConnectFrom, TConnectTo>(), "TConnectFrom and TConnectTo are not compatible", nameof(TConnectFrom));
            Ensure.That(AreGraphLookupFromAndToTypesCompatible<TStartWith, TConnectTo>(), "TStartWith and TConnectTo are not compatible", nameof(TStartWith));

            const string operatorName = "$graphLookup";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var inputSerializer = s;
                    var outputSerializer = options?.OutputSerializer ?? sr.GetSerializer<TOutput>();
                    var fromSerializer = options?.FromSerializer ?? sr.GetSerializer<TFrom>();
                    var asElementSerializer = options?.AsElementSerializer ?? sr.GetSerializer<TAsElement>();
                    var renderedConnectFromField = connectFromField.Render(fromSerializer, sr, linqProvider);
                    var renderedConnectToField = connectToField.Render(fromSerializer, sr, linqProvider);
                    var renderedStartWith = startWith.Render(inputSerializer, sr, linqProvider);
                    var renderedAs = @as.Render(outputSerializer, sr, linqProvider);
                    var renderedDepthField = depthField?.Render(asElementSerializer, sr, linqProvider);
                    var renderedRestrictSearchWithMatch = options?.RestrictSearchWithMatch?.Render(fromSerializer, sr, linqProvider);
                    var document = new BsonDocument
                    {
                        { operatorName, new BsonDocument
                            {
                                { "from", from.CollectionNamespace.CollectionName },
                                { "connectFromField", renderedConnectFromField.FieldName },
                                { "connectToField", renderedConnectToField.FieldName },
                                { "startWith", renderedStartWith },
                                { "as", renderedAs.FieldName },
                                { "depthField", () => renderedDepthField.FieldName, renderedDepthField != null },
                                { "maxDepth", () => options.MaxDepth.Value, options != null && options.MaxDepth.HasValue },
                                { "restrictSearchWithMatch", renderedRestrictSearchWithMatch, renderedRestrictSearchWithMatch != null }
                            }
                        }
                    };
                    return new RenderedPipelineStageDefinition<TOutput>(operatorName, document, outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $graphLookup stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TFrom">The type of the from documents.</typeparam>
        /// <typeparam name="TConnectFrom">The type of the connect from field (must be either TConnectTo or a type that implements IEnumerable{TConnectTo}).</typeparam>
        /// <typeparam name="TConnectTo">The type of the connect to field.</typeparam>
        /// <typeparam name="TStartWith">The type of the start with expression (must be either TConnectTo or a type that implements IEnumerable{TConnectTo}).</typeparam>
        /// <typeparam name="TAs">The type of the as field.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="from">The from collection.</param>
        /// <param name="connectFromField">The connect from field.</param>
        /// <param name="connectToField">The connect to field.</param>
        /// <param name="startWith">The start with value.</param>
        /// <param name="as">The as field.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> GraphLookup<TInput, TFrom, TConnectFrom, TConnectTo, TStartWith, TAs, TOutput>(
            IMongoCollection<TFrom> from,
            FieldDefinition<TFrom, TConnectFrom> connectFromField,
            FieldDefinition<TFrom, TConnectTo> connectToField,
            AggregateExpressionDefinition<TInput, TStartWith> startWith,
            FieldDefinition<TOutput, TAs> @as,
            AggregateGraphLookupOptions<TFrom, TFrom, TOutput> options = null)
                where TAs : IEnumerable<TFrom>
        {
            return GraphLookup(from, connectFromField, connectToField, startWith, @as, null, options);
        }

        /// <summary>
        /// Creates a $graphLookup stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TFrom">The type of the from documents.</typeparam>
        /// <param name="from">The from collection.</param>
        /// <param name="connectFromField">The connect from field.</param>
        /// <param name="connectToField">The connect to field.</param>
        /// <param name="startWith">The start with value.</param>
        /// <param name="as">The as field.</param>
        /// <param name="depthField">The depth field.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static PipelineStageDefinition<TInput, BsonDocument> GraphLookup<TInput, TFrom>(
            IMongoCollection<TFrom> from,
            FieldDefinition<TFrom, BsonValue> connectFromField,
            FieldDefinition<TFrom, BsonValue> connectToField,
            AggregateExpressionDefinition<TInput, BsonValue> startWith,
            FieldDefinition<BsonDocument, IEnumerable<BsonDocument>> @as,
            FieldDefinition<BsonDocument, int> depthField = null)
        {
            return GraphLookup(from, connectFromField, connectToField, startWith, @as, depthField, null);
        }

        /// <summary>
        /// Creates a $graphLookup stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TFrom">The type of the from documents.</typeparam>
        /// <typeparam name="TConnectFrom">The type of the connect from field (must be either TConnectTo or a type that implements IEnumerable{TConnectTo}).</typeparam>
        /// <typeparam name="TConnectTo">The type of the connect to field.</typeparam>
        /// <typeparam name="TStartWith">The type of the start with expression (must be either TConnectTo or a type that implements IEnumerable{TConnectTo}).</typeparam>
        /// <typeparam name="TAs">The type of the as field.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="from">The from collection.</param>
        /// <param name="connectFromField">The connect from field.</param>
        /// <param name="connectToField">The connect to field.</param>
        /// <param name="startWith">The start with value.</param>
        /// <param name="as">The as field.</param>
        /// <param name="options">The options.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> GraphLookup<TInput, TFrom, TConnectFrom, TConnectTo, TStartWith, TAs, TOutput>(
            IMongoCollection<TFrom> from,
            Expression<Func<TFrom, TConnectFrom>> connectFromField,
            Expression<Func<TFrom, TConnectTo>> connectToField,
            Expression<Func<TInput, TStartWith>> startWith,
            Expression<Func<TOutput, TAs>> @as,
            AggregateGraphLookupOptions<TFrom, TFrom, TOutput> options = null,
            ExpressionTranslationOptions translationOptions = null)
                where TAs : IEnumerable<TFrom>
        {
            Ensure.IsNotNull(connectFromField, nameof(connectFromField));
            Ensure.IsNotNull(connectToField, nameof(connectToField));
            Ensure.IsNotNull(startWith, nameof(startWith));
            Ensure.IsNotNull(@as, nameof(@as));
            return GraphLookup(
                from,
                new ExpressionFieldDefinition<TFrom, TConnectFrom>(connectFromField),
                new ExpressionFieldDefinition<TFrom, TConnectTo>(connectToField),
                new ExpressionAggregateExpressionDefinition<TInput, TStartWith>(startWith, translationOptions),
                new ExpressionFieldDefinition<TOutput, TAs>(@as),
                options);
        }

        /// <summary>
        /// Creates a $graphLookup stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TFrom">The type of the from documents.</typeparam>
        /// <typeparam name="TConnectFrom">The type of the connect from field (must be either TConnectTo or a type that implements IEnumerable{TConnectTo}).</typeparam>
        /// <typeparam name="TConnectTo">The type of the connect to field.</typeparam>
        /// <typeparam name="TStartWith">The type of the start with expression (must be either TConnectTo or a type that implements IEnumerable{TConnectTo}).</typeparam>
        /// <typeparam name="TAsElement">The type of the as field elements.</typeparam>
        /// <typeparam name="TAs">The type of the as field.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="from">The from collection.</param>
        /// <param name="connectFromField">The connect from field.</param>
        /// <param name="connectToField">The connect to field.</param>
        /// <param name="startWith">The start with value.</param>
        /// <param name="as">The as field.</param>
        /// <param name="depthField">The depth field.</param>
        /// <param name="options">The options.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> GraphLookup<TInput, TFrom, TConnectFrom, TConnectTo, TStartWith, TAsElement, TAs, TOutput>(
            IMongoCollection<TFrom> from,
            Expression<Func<TFrom, TConnectFrom>> connectFromField,
            Expression<Func<TFrom, TConnectTo>> connectToField,
            Expression<Func<TInput, TStartWith>> startWith,
            Expression<Func<TOutput, TAs>> @as,
            Expression<Func<TAsElement, int>> depthField,
            AggregateGraphLookupOptions<TFrom, TAsElement, TOutput> options = null,
            ExpressionTranslationOptions translationOptions = null)
                where TAs : IEnumerable<TAsElement>
        {
            Ensure.IsNotNull(connectFromField, nameof(connectFromField));
            Ensure.IsNotNull(connectToField, nameof(connectToField));
            Ensure.IsNotNull(startWith, nameof(startWith));
            Ensure.IsNotNull(@as, nameof(@as));
            Ensure.IsNotNull(depthField, nameof(depthField));
            return GraphLookup(
                from,
                new ExpressionFieldDefinition<TFrom, TConnectFrom>(connectFromField),
                new ExpressionFieldDefinition<TFrom, TConnectTo>(connectToField),
                new ExpressionAggregateExpressionDefinition<TInput, TStartWith>(startWith, translationOptions),
                new ExpressionFieldDefinition<TOutput, TAs>(@as),
                new ExpressionFieldDefinition<TAsElement, int>(depthField),
                options);
        }

        /// <summary>
        /// Creates a $group stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="group">The group projection.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Group<TInput, TOutput>(
            ProjectionDefinition<TInput, TOutput> group)
        {
            Ensure.IsNotNull(group, nameof(group));

            const string operatorName = "$group";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var renderedProjection = group.Render(s, sr, linqProvider);
                    return new RenderedPipelineStageDefinition<TOutput>(operatorName, new BsonDocument(operatorName, renderedProjection.Document), renderedProjection.ProjectionSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $group stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="group">The group projection.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, BsonDocument> Group<TInput>(
            ProjectionDefinition<TInput, BsonDocument> group)
        {
            return Group<TInput, BsonDocument>(group);
        }

        /// <summary>
        /// Creates a $group stage (this method can only be used with LINQ2).
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="value">The value field.</param>
        /// <param name="group">The group projection.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        /// <remarks>This method can only be used with LINQ2 but that can't be verified until Render is called.</remarks>
        public static PipelineStageDefinition<TInput, TOutput> Group<TInput, TValue, TOutput>(
            Expression<Func<TInput, TValue>> value,
            Expression<Func<IGrouping<TValue, TInput>, TOutput>> group,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(value, nameof(value));
            Ensure.IsNotNull(group, nameof(group));
            return new GroupWithOutputExpressionStageDefinition<TInput, TValue, TOutput>(value, group);
        }

        /// <summary>
        /// Creates a $limit stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="limit">The limit.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Limit<TInput>(
            long limit)
        {
            Ensure.IsGreaterThanZero(limit, nameof(limit));
            return new BsonDocumentPipelineStageDefinition<TInput, TInput>(new BsonDocument("$limit", limit));
        }

        /// <summary>
        /// Creates a $lookup stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TForeignDocument">The type of the foreign collection documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="foreignCollection">The foreign collection.</param>
        /// <param name="localField">The local field.</param>
        /// <param name="foreignField">The foreign field.</param>
        /// <param name="as">The "as" field.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Lookup<TInput, TForeignDocument, TOutput>(
            IMongoCollection<TForeignDocument> foreignCollection,
            FieldDefinition<TInput> localField,
            FieldDefinition<TForeignDocument> foreignField,
            FieldDefinition<TOutput> @as,
            AggregateLookupOptions<TForeignDocument, TOutput> options = null)
        {
            Ensure.IsNotNull(foreignCollection, nameof(foreignCollection));
            Ensure.IsNotNull(localField, nameof(localField));
            Ensure.IsNotNull(foreignField, nameof(foreignField));
            Ensure.IsNotNull(@as, nameof(@as));

            options = options ?? new AggregateLookupOptions<TForeignDocument, TOutput>();
            const string operatorName = "$lookup";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (inputSerializer, sr, linqProvider) =>
                {
                    var foreignSerializer = options.ForeignSerializer ?? (inputSerializer as IBsonSerializer<TForeignDocument>) ?? sr.GetSerializer<TForeignDocument>();
                    var outputSerializer = options.ResultSerializer ?? (inputSerializer as IBsonSerializer<TOutput>) ?? sr.GetSerializer<TOutput>();
                    return new RenderedPipelineStageDefinition<TOutput>(
                        operatorName, new BsonDocument(operatorName, new BsonDocument
                        {
                            { "from", foreignCollection.CollectionNamespace.CollectionName },
                            { "localField", localField.Render(inputSerializer, sr, linqProvider).FieldName },
                            { "foreignField", foreignField.Render(foreignSerializer, sr, linqProvider).FieldName },
                            { "as", @as.Render(outputSerializer, sr, linqProvider).FieldName }
                        }),
                        outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $lookup stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TForeignDocument">The type of the foreign collection documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="foreignCollection">The foreign collection.</param>
        /// <param name="localField">The local field.</param>
        /// <param name="foreignField">The foreign field.</param>
        /// <param name="as">The "as" field.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Lookup<TInput, TForeignDocument, TOutput>(
            IMongoCollection<TForeignDocument> foreignCollection,
            Expression<Func<TInput, object>> localField,
            Expression<Func<TForeignDocument, object>> foreignField,
            Expression<Func<TOutput, object>> @as,
            AggregateLookupOptions<TForeignDocument, TOutput> options = null)
        {
            Ensure.IsNotNull(localField, nameof(localField));
            Ensure.IsNotNull(foreignField, nameof(foreignField));
            Ensure.IsNotNull(@as, nameof(@as));
            return Lookup(
                foreignCollection,
                new ExpressionFieldDefinition<TInput>(localField),
                new ExpressionFieldDefinition<TForeignDocument>(foreignField),
                new ExpressionFieldDefinition<TOutput>(@as),
                options);
        }

        /// <summary>
        /// Creates a $lookup stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TForeignDocument">The type of the foreign collection documents.</typeparam>
        /// <typeparam name="TAsElement">The type of the as field elements.</typeparam>
        /// <typeparam name="TAs">The type of the as field.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="foreignCollection">The foreign collection.</param>
        /// <param name="let">The "let" definition.</param>
        /// <param name="lookupPipeline">The lookup pipeline.</param>
        /// <param name="as">The as field in <typeparamref name="TOutput" /> in which to place the results of the lookup pipeline.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Lookup<TInput, TForeignDocument, TAsElement, TAs, TOutput>(
            IMongoCollection<TForeignDocument> foreignCollection,
            BsonDocument let,
            PipelineDefinition<TForeignDocument, TAsElement> lookupPipeline,
            FieldDefinition<TOutput, TAs> @as,
            AggregateLookupOptions<TForeignDocument, TOutput> options = null)
            where TAs : IEnumerable<TAsElement>
        {
            Ensure.IsNotNull(foreignCollection, nameof(foreignCollection));
            Ensure.IsNotNull(lookupPipeline, nameof(lookupPipeline));
            Ensure.IsNotNull(@as, nameof(@as));

            options = options ?? new AggregateLookupOptions<TForeignDocument, TOutput>();
            const string operatorName = "$lookup";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (inputSerializer, sr, linqProvider) =>
                {
                    var foreignSerializer = options.ForeignSerializer ?? foreignCollection.DocumentSerializer ?? inputSerializer as IBsonSerializer<TForeignDocument> ?? sr.GetSerializer<TForeignDocument>();
                    var outputSerializer = options.ResultSerializer ?? inputSerializer as IBsonSerializer<TOutput> ?? sr.GetSerializer<TOutput>();
                    var lookupPipelineDocuments = new BsonArray(lookupPipeline.Render(foreignSerializer, sr, linqProvider).Documents);

                    var lookupBody = new BsonDocument
                    {
                        { "from", foreignCollection.CollectionNamespace.CollectionName },
                        { "let", let, let != null },
                        { "pipeline", lookupPipelineDocuments },
                        { "as", @as.Render(outputSerializer, sr, linqProvider).FieldName }
                    };

                    return new RenderedPipelineStageDefinition<TOutput>(operatorName, new BsonDocument(operatorName, lookupBody), outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $lookup stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TForeignDocument">The type of the foreign collection documents.</typeparam>
        /// <typeparam name="TAsElement">The type of the as field elements.</typeparam>
        /// <typeparam name="TAs">The type of the as field.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="foreignCollection">The foreign collection.</param>
        /// <param name="let">The "let" definition.</param>
        /// <param name="lookupPipeline">The lookup pipeline.</param>
        /// <param name="as">The as field in <typeparamref name="TOutput" /> in which to place the results of the lookup pipeline.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Lookup<TInput, TForeignDocument, TAsElement, TAs, TOutput>(
            IMongoCollection<TForeignDocument> foreignCollection,
            BsonDocument let,
            PipelineDefinition<TForeignDocument, TAsElement> lookupPipeline,
            Expression<Func<TOutput, TAs>> @as,
            AggregateLookupOptions<TForeignDocument, TOutput> options = null)
            where TAs : IEnumerable<TAsElement>
        {
            Ensure.IsNotNull(foreignCollection, nameof(foreignCollection));
            Ensure.IsNotNull(lookupPipeline, nameof(lookupPipeline));
            Ensure.IsNotNull(@as, nameof(@as));

            return Lookup<TInput, TForeignDocument, TAsElement, TAs, TOutput>(
                foreignCollection,
                let,
                lookupPipeline,
                new ExpressionFieldDefinition<TOutput, TAs>(@as));
        }

        /// <summary>
        /// Creates a $match stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="filter">The filter.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Match<TInput>(
            FilterDefinition<TInput> filter)
        {
            Ensure.IsNotNull(filter, nameof(filter));

            const string operatorName = "$match";
            var stage = new DelegatedPipelineStageDefinition<TInput, TInput>(
                operatorName,
                (s, sr, linqProvider) => new RenderedPipelineStageDefinition<TInput>(operatorName, new BsonDocument(operatorName, filter.Render(s, sr, linqProvider)), s));

            return stage;
        }

        /// <summary>
        /// Creates a $match stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="filter">The filter.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Match<TInput>(
            Expression<Func<TInput, bool>> filter)
        {
            Ensure.IsNotNull(filter, nameof(filter));
            return Match(new ExpressionFilterDefinition<TInput>(filter));
        }

        /// <summary>
        /// Creates a $merge stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="outputCollection">The output collection.</param>
        /// <param name="mergeOptions">The merge options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Merge<TInput, TOutput>(
            IMongoCollection<TOutput> outputCollection,
            MergeStageOptions<TOutput> mergeOptions)
        {
            Ensure.IsNotNull(outputCollection, nameof(outputCollection));
            Ensure.IsNotNull(mergeOptions, nameof(mergeOptions));

            if (mergeOptions.LetVariables != null && mergeOptions.WhenMatched != MergeStageWhenMatched.Pipeline)
            {
                throw new ArgumentException("LetVariables can only be set when WhenMatched == Pipeline.");
            }

            if (mergeOptions.WhenMatchedPipeline == null)
            {
                if (mergeOptions.WhenMatched == MergeStageWhenMatched.Pipeline)
                {
                    throw new ArgumentException("WhenMatchedPipeline is required when WhenMatched == Pipeline.");
                }
            }
            else
            {
                if (mergeOptions.WhenMatched != MergeStageWhenMatched.Pipeline)
                {
                    throw new ArgumentException("WhenMatchedPipeline can only be set when WhenMatched == Pipeline.");
                }
            }

            const string operatorName = "$merge";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (inputSerializer, serializerRegistry, linqProvider) =>
                {
                    var outputSerializer = mergeOptions.OutputSerializer ?? (inputSerializer as IBsonSerializer<TOutput>) ?? serializerRegistry.GetSerializer<TOutput>();

                    var outputCollectionNamespace = outputCollection.CollectionNamespace;
                    var outputDatabaseNamespace = outputCollectionNamespace.DatabaseNamespace;
                    var renderedInto = new BsonDocument
                    {
                        { "db", outputDatabaseNamespace.DatabaseName },
                        { "coll", outputCollectionNamespace.CollectionName }
                    };

                    BsonValue renderedOn = null;
                    if (mergeOptions.OnFieldNames != null)
                    {
                        if (mergeOptions.OnFieldNames.Count == 1)
                        {
                            renderedOn = mergeOptions.OnFieldNames.Single();
                        }
                        else
                        {
                            renderedOn = new BsonArray(mergeOptions.OnFieldNames.Select(n => BsonString.Create(n)));
                        }
                    }

                    BsonValue renderedWhenMatched = null;
                    if (mergeOptions.WhenMatched.HasValue)
                    {
                        var whenMatched = mergeOptions.WhenMatched.Value;
                        if (whenMatched == MergeStageWhenMatched.Pipeline)
                        {
                            var renderedPipeline = mergeOptions.WhenMatchedPipeline.Render(outputSerializer, serializerRegistry, linqProvider);
                            renderedWhenMatched = new BsonArray(renderedPipeline.Documents);
                        }
                        else
                        {
                            renderedWhenMatched = MongoUtils.ToCamelCase(whenMatched.ToString());
                        }
                    }

                    BsonString renderedWhenNotMatched = null;
                    if (mergeOptions.WhenNotMatched.HasValue)
                    {
                        var whenNotMatched = mergeOptions.WhenNotMatched;
                        renderedWhenNotMatched = MongoUtils.ToCamelCase(whenNotMatched.ToString());
                    }

                    var renderedMerge = new BsonDocument
                    {
                        { "into", renderedInto },
                        { "on", renderedOn, renderedOn != null },
                        { "let", mergeOptions.LetVariables, mergeOptions.LetVariables != null },
                        { "whenMatched", renderedWhenMatched, renderedWhenMatched != null },
                        { "whenNotMatched", renderedWhenNotMatched, renderedWhenNotMatched != null }
                    };

                    var renderedStage = new BsonDocument(operatorName, renderedMerge);

                    return new RenderedPipelineStageDefinition<TOutput>(operatorName, renderedStage, outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Create a $match stage that select documents of a sub type.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="outputSerializer">The output serializer.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> OfType<TInput, TOutput>(
            IBsonSerializer<TOutput> outputSerializer = null)
                where TOutput : TInput
        {
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(TOutput));
            if (discriminatorConvention == null)
            {
                var message = string.Format("OfType requires that a discriminator convention exist for type: {0}.", BsonUtils.GetFriendlyTypeName(typeof(TOutput)));
                throw new NotSupportedException(message);
            }

            var discriminatorValue = discriminatorConvention.GetDiscriminator(typeof(TInput), typeof(TOutput));
            var ofTypeFilter = new BsonDocument(discriminatorConvention.ElementName, discriminatorValue);

            const string operatorName = "$match";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    return new RenderedPipelineStageDefinition<TOutput>(
                        operatorName,
                        new BsonDocument(operatorName, ofTypeFilter),
                        outputSerializer ?? (s as IBsonSerializer<TOutput>) ?? sr.GetSerializer<TOutput>());
                });

            return stage;
        }

        /// <summary>
        /// Creates a $out stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="outputCollection">The output collection.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Out<TInput>(
            IMongoCollection<TInput> outputCollection)
        {
            Ensure.IsNotNull(outputCollection, nameof(outputCollection));
            var outputDatabaseName = outputCollection.Database.DatabaseNamespace.DatabaseName;
            var outputCollectionName = outputCollection.CollectionNamespace.CollectionName;
            var outDocument = new BsonDocument { { "db", outputDatabaseName }, { "coll", outputCollectionName } };
            return new BsonDocumentPipelineStageDefinition<TInput, TInput>(new BsonDocument("$out", outDocument));
        }

        /// <summary>
        /// Creates a $project stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="projection">The projection.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Project<TInput, TOutput>(
            ProjectionDefinition<TInput, TOutput> projection)
        {
            Ensure.IsNotNull(projection, nameof(projection));

            const string operatorName = "$project";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var renderedProjection = projection.Render(s, sr, linqProvider);
                    IEnumerable<BsonDocument> documents;
                    if (renderedProjection.Document == null)
                    {
                        // renderedProjection.Document will be null for x => x (and perhaps other cases also in the future)
                        documents = Array.Empty<BsonDocument>();
                    }
                    else
                    {
                        documents = new[] { new BsonDocument(operatorName, renderedProjection.Document) };
                    }
                    return new RenderedPipelineStageDefinition<TOutput>(operatorName, documents, renderedProjection.ProjectionSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $project stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="projection">The projection.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, BsonDocument> Project<TInput>(
            ProjectionDefinition<TInput, BsonDocument> projection)
        {
            return Project<TInput, BsonDocument>(projection);
        }

        /// <summary>
        /// Creates a $project stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="projection">The projection.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Project<TInput, TOutput>(
            Expression<Func<TInput, TOutput>> projection,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(projection, nameof(projection));
            return Project(new ExpressionProjectionDefinition<TInput, TOutput>(projection, translationOptions));
        }

        /// <summary>
        /// Creates a $search stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="searchDefinition">The search definition.</param>
        /// <param name="highlight">The highlight options.</param>
        /// <param name="indexName">The index name.</param>
        /// <param name="count">The count options.</param>
        /// <param name="returnStoredSource">
        /// Flag that specifies whether to perform a full document lookup on the backend database
        /// or return only stored source fields directly from Atlas Search.
        /// </param>
        /// <param name="scoreDetails">
        /// Flag that specifies whether to return a detailed breakdown
        /// of the score for each document in the result. 
        /// </param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Search<TInput>(
            SearchDefinition<TInput> searchDefinition,
            SearchHighlightOptions<TInput> highlight = null,
            string indexName = null,
            SearchCountOptions count = null,
            bool returnStoredSource = false,
            bool scoreDetails = false)
        {
            var searchOptions = new SearchOptions<TInput>()
            {
                CountOptions = count,
                Highlight = highlight,
                IndexName = indexName,
                ReturnStoredSource = returnStoredSource,
                ScoreDetails = scoreDetails
            };

            return Search(searchDefinition, searchOptions);
        }

        /// <summary>
        /// Creates a $search stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="searchDefinition">The search definition.</param>
        /// <param name="searchOptions">The search options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Search<TInput>(
            SearchDefinition<TInput> searchDefinition,
            SearchOptions<TInput> searchOptions)
        {
            Ensure.IsNotNull(searchDefinition, nameof(searchDefinition));

            const string operatorName = "$search";
            var stage = new DelegatedPipelineStageDefinition<TInput, TInput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var renderContext = new SearchDefinitionRenderContext<TInput>(s, sr);
                    var renderedSearchDefinition = searchDefinition.Render(renderContext);
                    renderedSearchDefinition.Add("highlight", () => searchOptions.Highlight.Render(renderContext), searchOptions.Highlight != null);
                    renderedSearchDefinition.Add("count", () => searchOptions.CountOptions.Render(), searchOptions.CountOptions != null);
                    renderedSearchDefinition.Add("sort", () => searchOptions.Sort.Render(s, sr), searchOptions.Sort != null);
                    renderedSearchDefinition.Add("index", searchOptions.IndexName, searchOptions.IndexName != null);
                    renderedSearchDefinition.Add("returnStoredSource", searchOptions.ReturnStoredSource, searchOptions.ReturnStoredSource);
                    renderedSearchDefinition.Add("scoreDetails", searchOptions.ScoreDetails, searchOptions.ScoreDetails);
                    renderedSearchDefinition.Add("tracking", () => searchOptions.Tracking.Render(), searchOptions.Tracking != null);

                    var document = new BsonDocument(operatorName, renderedSearchDefinition);
                    return new RenderedPipelineStageDefinition<TInput>(operatorName, document, s);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $searchMeta stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="searchDefinition">The search definition.</param>
        /// <param name="indexName">The index name.</param>
        /// <param name="count">The count options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, SearchMetaResult> SearchMeta<TInput>(
            SearchDefinition<TInput> searchDefinition,
            string indexName = null,
            SearchCountOptions count = null)
        {
            Ensure.IsNotNull(searchDefinition, nameof(searchDefinition));

            const string operatorName = "$searchMeta";
            var stage = new DelegatedPipelineStageDefinition<TInput, SearchMetaResult>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var renderedSearchDefinition = searchDefinition.Render(new(s, sr));
                    renderedSearchDefinition.Add("count", () => count.Render(), count != null);
                    renderedSearchDefinition.Add("index", indexName, indexName != null);

                    var document = new BsonDocument(operatorName, renderedSearchDefinition);
                    return new RenderedPipelineStageDefinition<SearchMetaResult>(
                        operatorName,
                        document,
                        sr.GetSerializer<SearchMetaResult>());
                });

            return stage;
        }

        /// <summary>
        /// Creates a $replaceRoot stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="newRoot">The new root.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> ReplaceRoot<TInput, TOutput>(
            AggregateExpressionDefinition<TInput, TOutput> newRoot)
        {
            Ensure.IsNotNull(newRoot, nameof(newRoot));

            const string operatorName = "$replaceRoot";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var document = new BsonDocument(operatorName, new BsonDocument("newRoot", newRoot.Render(s, sr, linqProvider)));
                    var outputSerializer = sr.GetSerializer<TOutput>();
                    return new RenderedPipelineStageDefinition<TOutput>(operatorName, document, outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $replaceRoot stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="newRoot">The new root.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> ReplaceRoot<TInput, TOutput>(
            Expression<Func<TInput, TOutput>> newRoot,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(newRoot, nameof(newRoot));
            return ReplaceRoot(new ExpressionAggregateExpressionDefinition<TInput, TOutput>(newRoot, translationOptions));
        }

        /// <summary>
        /// Creates a $replaceWith stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="newRoot">The new root.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> ReplaceWith<TInput, TOutput>(
            AggregateExpressionDefinition<TInput, TOutput> newRoot)
        {
            Ensure.IsNotNull(newRoot, nameof(newRoot));

            const string operatorName = "$replaceWith";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var document = new BsonDocument(operatorName, newRoot.Render(s, sr, linqProvider));
                    var outputSerializer = sr.GetSerializer<TOutput>();
                    return new RenderedPipelineStageDefinition<TOutput>(operatorName, document, outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $replaceWith stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="newRoot">The new root.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> ReplaceWith<TInput, TOutput>(
            Expression<Func<TInput, TOutput>> newRoot,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(newRoot, nameof(newRoot));
            return ReplaceWith(new ExpressionAggregateExpressionDefinition<TInput, TOutput>(newRoot, translationOptions));
        }

        /// <summary>
        /// Creates a $set stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="fields">The fields to set.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Set<TInput>(
            SetFieldDefinitions<TInput> fields)
        {
            Ensure.IsNotNull(fields, nameof(fields));

            const string operatorName = "$set";
            var stage = new DelegatedPipelineStageDefinition<TInput, TInput>(
                operatorName,
                (inputSerializer, sr, linqProvider) =>
                {
                    var renderedFields = fields.Render(inputSerializer, sr, linqProvider);
                    var stage = new BsonDocument(operatorName, renderedFields);
                    return new RenderedPipelineStageDefinition<TInput>(operatorName, stage, inputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $set stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TFields">The type of object specifying the fields to set.</typeparam>
        /// <param name="fields">The fields to set.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Set<TInput, TFields>(
            Expression<Func<TInput, TFields>> fields)
        {
            var fieldsDefinition = new ExpressionSetFieldDefinitions<TInput, TFields>(fields);
            return Set(fieldsDefinition);
        }

        /// <summary>
        /// Create a $setWindowFields stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TWindowFields">The type of the added window fields.</typeparam>
        /// <param name="output">The window fields expression.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, BsonDocument> SetWindowFields<TInput, TWindowFields>(
            AggregateExpressionDefinition<ISetWindowFieldsPartition<TInput>, TWindowFields> output)
        {
            Ensure.IsNotNull(output, nameof(output));

            const string operatorName = "$setWindowFields";
            var stage = new DelegatedPipelineStageDefinition<TInput, BsonDocument>(
                operatorName,
                (inputSerializer, sr, linqProvider) =>
                {
                    var partitionSerializer = new ISetWindowFieldsPartitionSerializer<TInput>(inputSerializer);
                    var document = new BsonDocument
                    {
                        { "$setWindowFields", new BsonDocument
                            {
                                { "output", output.Render(partitionSerializer, sr, linqProvider) }
                            }
                        }
                    };
                    var outputSerializer = sr.GetSerializer<BsonDocument>();
                    return new RenderedPipelineStageDefinition<BsonDocument>(operatorName, document, outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Create a $setWindowFields stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TPartitionBy">The type of the value to partition by.</typeparam>
        /// <typeparam name="TWindowFields">The type of the added window fields.</typeparam>
        /// <param name="partitionBy">The partitionBy expression.</param>
        /// <param name="output">The window fields expression.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, BsonDocument> SetWindowFields<TInput, TPartitionBy, TWindowFields>(
            AggregateExpressionDefinition<TInput, TPartitionBy> partitionBy,
            AggregateExpressionDefinition<ISetWindowFieldsPartition<TInput>, TWindowFields> output)
        {
            Ensure.IsNotNull(partitionBy, nameof(partitionBy));
            Ensure.IsNotNull(output, nameof(output));

            const string operatorName = "$setWindowFields";
            var stage = new DelegatedPipelineStageDefinition<TInput, BsonDocument>(
                operatorName,
                (inputSerializer, sr, linqProvider) =>
                {
                    var partitionSerializer = new ISetWindowFieldsPartitionSerializer<TInput>(inputSerializer);
                    var document = new BsonDocument
                    {
                        { "$setWindowFields", new BsonDocument
                            {
                                { "partitionBy", partitionBy.Render(inputSerializer, sr, linqProvider) },
                                { "output", output.Render(partitionSerializer, sr, linqProvider) }
                            }
                        }
                    };
                    var outputSerializer = sr.GetSerializer<BsonDocument>();
                    return new RenderedPipelineStageDefinition<BsonDocument>(operatorName, document, outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Create a $setWindowFields stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TPartitionBy">The type of the value to partition by.</typeparam>
        /// <typeparam name="TWindowFields">The type of the added window fields.</typeparam>
        /// <param name="partitionBy">The partitionBy expression.</param>
        /// <param name="sortBy">The sortBy expression.</param>
        /// <param name="output">The window fields expression.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, BsonDocument> SetWindowFields<TInput, TPartitionBy, TWindowFields>(
            AggregateExpressionDefinition<TInput, TPartitionBy> partitionBy,
            SortDefinition<TInput> sortBy,
            AggregateExpressionDefinition<ISetWindowFieldsPartition<TInput>, TWindowFields> output)
        {
            Ensure.IsNotNull(partitionBy, nameof(partitionBy));
            Ensure.IsNotNull(sortBy, nameof(sortBy));
            Ensure.IsNotNull(output, nameof(output));

            const string operatorName = "$setWindowFields";
            var stage = new DelegatedPipelineStageDefinition<TInput, BsonDocument>(
                operatorName,
                (inputSerializer, sr, linqProvider) =>
                {
                    var partitionSerializer = new ISetWindowFieldsPartitionSerializer<TInput>(inputSerializer);
                    var document = new BsonDocument
                    {
                        { "$setWindowFields", new BsonDocument
                            {
                                { "partitionBy", partitionBy.Render(inputSerializer, sr, linqProvider) },
                                { "sortBy", sortBy.Render(inputSerializer, sr, linqProvider) },
                                { "output", output.Render(partitionSerializer, sr, linqProvider) }
                            }
                        }
                    };
                    var outputSerializer = sr.GetSerializer<BsonDocument>();
                    return new RenderedPipelineStageDefinition<BsonDocument>(operatorName, document, outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Create a $setWindowFields stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TWindowFields">The type of the added window fields.</typeparam>
        /// <param name="output">The window fields expression.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, BsonDocument> SetWindowFields<TInput, TWindowFields>(
            Expression<Func<ISetWindowFieldsPartition<TInput>, TWindowFields>> output,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(output, nameof(output));
            return SetWindowFields(
                new ExpressionAggregateExpressionDefinition<ISetWindowFieldsPartition<TInput>, TWindowFields>(output, translationOptions));
        }

        /// <summary>
        /// Create a $setWindowFields stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TPartitionBy">The type of the value to partition by.</typeparam>
        /// <typeparam name="TWindowFields">The type of the added window fields.</typeparam>
        /// <param name="partitionBy">The partitionBy expression.</param>
        /// <param name="output">The window fields expression.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, BsonDocument> SetWindowFields<TInput, TPartitionBy, TWindowFields>(
            Expression<Func<TInput, TPartitionBy>> partitionBy,
            Expression<Func<ISetWindowFieldsPartition<TInput>, TWindowFields>> output,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(partitionBy, nameof(partitionBy));
            Ensure.IsNotNull(output, nameof(output));
            return SetWindowFields(
                new ExpressionAggregateExpressionDefinition<TInput, TPartitionBy>(partitionBy, translationOptions),
                new ExpressionAggregateExpressionDefinition<ISetWindowFieldsPartition<TInput>, TWindowFields>(output, translationOptions));
        }

        /// <summary>
        /// Create a $setWindowFields stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TPartitionBy">The type of the value to partition by.</typeparam>
        /// <typeparam name="TWindowFields">The type of the added window fields.</typeparam>
        /// <param name="partitionBy">The partitionBy expression.</param>
        /// <param name="sortBy">The sortBy expression.</param>
        /// <param name="output">The window fields expression.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, BsonDocument> SetWindowFields<TInput, TPartitionBy, TWindowFields>(
            Expression<Func<TInput, TPartitionBy>> partitionBy,
            SortDefinition<TInput> sortBy,
            Expression<Func<ISetWindowFieldsPartition<TInput>, TWindowFields>> output,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(partitionBy, nameof(partitionBy));
            Ensure.IsNotNull(sortBy, nameof(sortBy));
            Ensure.IsNotNull(output, nameof(output));

            var contextData = new TranslationContextData().With("SortBy", sortBy);

            return SetWindowFields(
                new ExpressionAggregateExpressionDefinition<TInput, TPartitionBy>(partitionBy, translationOptions),
                sortBy,
                new ExpressionAggregateExpressionDefinition<ISetWindowFieldsPartition<TInput>, TWindowFields>(output, translationOptions, contextData));
        }

        /// <summary>
        /// Creates a $skip stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="skip">The skip.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Skip<TInput>(
            long skip)
        {
            Ensure.IsGreaterThanOrEqualToZero(skip, nameof(skip));
            return new BsonDocumentPipelineStageDefinition<TInput, TInput>(new BsonDocument("$skip", skip));
        }

        /// <summary>
        /// Creates a $sort stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="sort">The sort.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> Sort<TInput>(
            SortDefinition<TInput> sort)
        {
            Ensure.IsNotNull(sort, nameof(sort));
            return new SortPipelineStageDefinition<TInput>(sort);
        }

        /// <summary>
        /// Creates a $sortByCount stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="value">The value expression.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, AggregateSortByCountResult<TValue>> SortByCount<TInput, TValue>(
            AggregateExpressionDefinition<TInput, TValue> value)
        {
            Ensure.IsNotNull(value, nameof(value));

            const string operatorName = "$sortByCount";
            var stage = new DelegatedPipelineStageDefinition<TInput, AggregateSortByCountResult<TValue>>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var outputSerializer = sr.GetSerializer<AggregateSortByCountResult<TValue>>();
                    return new RenderedPipelineStageDefinition<AggregateSortByCountResult<TValue>>(operatorName, new BsonDocument(operatorName, value.Render(s, sr, linqProvider)), outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates a $sortByCount stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="translationOptions">The translation options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, AggregateSortByCountResult<TValue>> SortByCount<TInput, TValue>(
            Expression<Func<TInput, TValue>> value,
            ExpressionTranslationOptions translationOptions = null)
        {
            Ensure.IsNotNull(value, nameof(value));
            return SortByCount(new ExpressionAggregateExpressionDefinition<TInput, TValue>(value, translationOptions));
        }

        /// <summary>
        /// Creates a $unionWith stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TWith">The type of the with collection documents.</typeparam>
        /// <param name="withCollection">The with collection.</param>
        /// <param name="withPipeline">The with pipeline.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> UnionWith<TInput, TWith>(
            IMongoCollection<TWith> withCollection,
            PipelineDefinition<TWith, TInput> withPipeline = null)
        {
            Ensure.IsNotNull(withCollection, nameof(withCollection));
            if (withPipeline == null && typeof(TWith) != typeof(TInput))
            {
                throw new ArgumentException("The withPipeline cannot be null when TWith != TInput. A pipeline is required to transform the TWith documents to TInput documents.", nameof(withPipeline));
            }

            const string operatorName = "$unionWith";
            var stage = new DelegatedPipelineStageDefinition<TInput, TInput>(
                operatorName,
                (inputSerializer, sr, linqProvider) =>
                {
                    BsonArray withPipelineDocuments;
                    if (withPipeline != null)
                    {
                        var withSerializer = withCollection.DocumentSerializer ?? inputSerializer as IBsonSerializer<TWith> ?? sr.GetSerializer<TWith>();
                        withPipelineDocuments = new BsonArray(withPipeline.Render(withSerializer, sr, linqProvider).Documents);
                    }
                    else
                    {
                        withPipelineDocuments = null;
                    }

                    var unionWithBody = new BsonDocument
                    {
                        { "coll", withCollection.CollectionNamespace.CollectionName },
                        { "pipeline", withPipelineDocuments, withPipelineDocuments != null }
                    };

                    return new RenderedPipelineStageDefinition<TInput>(operatorName, new BsonDocument(operatorName, unionWithBody), inputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates an $unwind stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Unwind<TInput, TOutput>(
            FieldDefinition<TInput> field,
            AggregateUnwindOptions<TOutput> options = null)
        {
            Ensure.IsNotNull(field, nameof(field));
            options = options ?? new AggregateUnwindOptions<TOutput>();

            const string operatorName = "$unwind";
            var stage = new DelegatedPipelineStageDefinition<TInput, TOutput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var outputSerializer = options.ResultSerializer ?? (s as IBsonSerializer<TOutput>) ?? sr.GetSerializer<TOutput>();

                    var fieldName = "$" + field.Render(s, sr, linqProvider).FieldName;
                    string includeArrayIndexFieldName = null;
                    if (options.IncludeArrayIndex != null)
                    {
                        includeArrayIndexFieldName = options.IncludeArrayIndex.Render(outputSerializer, sr, linqProvider).FieldName;
                    }

                    BsonValue value = fieldName;
                    if (options.PreserveNullAndEmptyArrays.HasValue || includeArrayIndexFieldName != null)
                    {
                        value = new BsonDocument
                        {
                            { "path", fieldName },
                            { "preserveNullAndEmptyArrays", options.PreserveNullAndEmptyArrays, options.PreserveNullAndEmptyArrays.HasValue },
                            { "includeArrayIndex", includeArrayIndexFieldName, includeArrayIndexFieldName != null }
                        };
                    }
                    return new RenderedPipelineStageDefinition<TOutput>(
                        operatorName,
                        new BsonDocument(operatorName, value),
                        outputSerializer);
                });

            return stage;
        }

        /// <summary>
        /// Creates an $unwind stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="field">The field to unwind.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, BsonDocument> Unwind<TInput>(
            FieldDefinition<TInput> field,
            AggregateUnwindOptions<BsonDocument> options = null)
        {
            return Unwind<TInput, BsonDocument>(field, options);
        }

        /// <summary>
        /// Creates an $unwind stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="field">The field to unwind.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, BsonDocument> Unwind<TInput>(
            Expression<Func<TInput, object>> field,
            AggregateUnwindOptions<BsonDocument> options = null)
        {
            Ensure.IsNotNull(field, nameof(field));
            return Unwind(new ExpressionFieldDefinition<TInput>(field), options);
        }

        /// <summary>
        /// Creates an $unwind stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TOutput">The type of the output documents.</typeparam>
        /// <param name="field">The field to unwind.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TOutput> Unwind<TInput, TOutput>(
            Expression<Func<TInput, object>> field,
            AggregateUnwindOptions<TOutput> options = null)
        {
            Ensure.IsNotNull(field, nameof(field));
            return Unwind(new ExpressionFieldDefinition<TInput>(field), options);
        }

        /// <summary>
        /// Creates a $vectorSearch stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="queryVector">The query vector.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> VectorSearch<TInput, TField>(
            Expression<Func<TInput, TField>> field,
            QueryVector queryVector,
            int limit,
            VectorSearchOptions<TInput> options)
            => VectorSearch(
                new ExpressionFieldDefinition<TInput>(field),
                queryVector,
                limit,
                options);

        /// <summary>
        /// Creates a $vectorSearch stage.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="queryVector">The query vector.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="options">The options.</param>
        /// <returns>The stage.</returns>
        public static PipelineStageDefinition<TInput, TInput> VectorSearch<TInput>(
            FieldDefinition<TInput> field,
            QueryVector queryVector,
            int limit,
            VectorSearchOptions<TInput> options = null)
        {
            Ensure.IsNotNull(field, nameof(field));
            Ensure.IsNotNull(queryVector, nameof(queryVector));
            Ensure.IsGreaterThanZero(limit, nameof(limit));

            const string operatorName = "$vectorSearch";
            var stage = new DelegatedPipelineStageDefinition<TInput, TInput>(
                operatorName,
                (s, sr, linqProvider) =>
                {
                    var vectorSearchOperator = new BsonDocument
                    {
                        { "queryVector", queryVector.Array },
                        { "path", field.Render(s, sr, linqProvider).FieldName },
                        { "limit", limit },
                        { "numCandidates", options?.NumberOfCandidates ?? limit * 10 },
                        { "index", options?.IndexName ?? "default" },
                        { "filter", () => options?.Filter?.Render(s, sr, linqProvider), options?.Filter != null },
                    };

                    var document = new BsonDocument(operatorName, vectorSearchOperator);
                    return new RenderedPipelineStageDefinition<TInput>(operatorName, document, s);
                });

            return stage;
        }

        // private methods
        private static bool AreGraphLookupFromAndToTypesCompatible<TConnectFrom, TConnectTo>()
        {
            if (typeof(TConnectFrom) == typeof(TConnectTo))
            {
                return true;
            }

            var ienumerableTConnectTo = typeof(IEnumerable<>).MakeGenericType(typeof(TConnectTo));
            if (ienumerableTConnectTo.GetTypeInfo().IsAssignableFrom(typeof(TConnectFrom)))
            {
                return true;
            }

            var ienumerableTConnectFrom = typeof(IEnumerable<>).MakeGenericType(typeof(TConnectFrom));
            if (ienumerableTConnectFrom.GetTypeInfo().IsAssignableFrom(typeof(TConnectTo)))
            {
                return true;
            }

            return false;
        }
    }

    internal sealed class ExpressionBucketOutputProjection<TInput, TValue, TOutput> : ProjectionDefinition<TInput, TOutput>
    {
        private readonly Expression<Func<IGrouping<TValue, TInput>, TOutput>> _outputExpression;
        private readonly ExpressionTranslationOptions _translationOptions;
        private readonly Expression<Func<TInput, TValue>> _valueExpression;

        public ExpressionBucketOutputProjection(
            Expression<Func<TInput, TValue>> valueExpression,
            Expression<Func<IGrouping<TValue, TInput>, TOutput>> outputExpression,
            ExpressionTranslationOptions translationOptions)
        {
            _valueExpression = Ensure.IsNotNull(valueExpression, nameof(valueExpression));
            _outputExpression = Ensure.IsNotNull(outputExpression, nameof(outputExpression));
            _translationOptions = translationOptions; // can be null

        }

        public Expression<Func<IGrouping<TValue, TInput>, TOutput>> OutputExpression
        {
            get { return _outputExpression; }
        }

        public override RenderedProjectionDefinition<TOutput> Render(IBsonSerializer<TInput> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            if (linqProvider != LinqProvider.V2)
            {
                throw new InvalidOperationException("ExpressionBucketOutputProjection can only be used with LINQ2.");
            }

            return linqProvider.GetAdapter().TranslateExpressionToBucketOutputProjection(_valueExpression, _outputExpression, documentSerializer, serializerRegistry, _translationOptions);
        }

        internal override RenderedProjectionDefinition<TOutput> RenderForFind(IBsonSerializer<TInput> sourceSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            throw new InvalidOperationException();
        }
    }

    internal sealed class GroupExpressionProjection<TInput, TKey, TOutput> : ProjectionDefinition<TInput, TOutput>
    {
        private readonly Expression<Func<TInput, TKey>> _idExpression;
        private readonly Expression<Func<IGrouping<TKey, TInput>, TOutput>> _groupExpression;
        private readonly ExpressionTranslationOptions _translationOptions;

        public GroupExpressionProjection(Expression<Func<TInput, TKey>> idExpression, Expression<Func<IGrouping<TKey, TInput>, TOutput>> groupExpression, ExpressionTranslationOptions translationOptions)
        {
            _idExpression = Ensure.IsNotNull(idExpression, nameof(idExpression));
            _groupExpression = Ensure.IsNotNull(groupExpression, nameof(groupExpression));
            _translationOptions = translationOptions; // can be null
        }

        public Expression<Func<TInput, TKey>> IdExpression
        {
            get { return _idExpression; }
        }

        public Expression<Func<IGrouping<TKey, TInput>, TOutput>> GroupExpression
        {
            get { return _groupExpression; }
        }

        public override RenderedProjectionDefinition<TOutput> Render(IBsonSerializer<TInput> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            if (linqProvider != LinqProvider.V2)
            {
                throw new InvalidOperationException("The GroupExpressionProjection class can only be used with LINQ2.");
            }
            return linqProvider.GetAdapter().TranslateExpressionToGroupProjection(_idExpression, _groupExpression, documentSerializer, serializerRegistry, _translationOptions);
        }

        internal override RenderedProjectionDefinition<TOutput> RenderForFind(IBsonSerializer<TInput> sourceSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            throw new InvalidOperationException();
        }
    }

    internal sealed class ExpressionProjectionDefinition<TInput, TOutput> : ProjectionDefinition<TInput, TOutput>
    {
        private readonly Expression<Func<TInput, TOutput>> _expression;
        private readonly ExpressionTranslationOptions _translationOptions;

        public ExpressionProjectionDefinition(Expression<Func<TInput, TOutput>> expression, ExpressionTranslationOptions translationOptions)
        {
            _expression = Ensure.IsNotNull(expression, nameof(expression));
            _translationOptions = translationOptions; // can be null
        }

        public Expression<Func<TInput, TOutput>> Expression
        {
            get { return _expression; }
        }

        public override RenderedProjectionDefinition<TOutput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            return linqProvider.GetAdapter().TranslateExpressionToProjection(_expression, inputSerializer, serializerRegistry, _translationOptions);
        }

        internal override RenderedProjectionDefinition<TOutput> RenderForFind(IBsonSerializer<TInput> sourceSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            return linqProvider.GetAdapter().TranslateExpressionToFindProjection(_expression, sourceSerializer, serializerRegistry);
        }
    }

    internal class SortPipelineStageDefinition<TInput> : PipelineStageDefinition<TInput, TInput>
    {
        public SortPipelineStageDefinition(SortDefinition<TInput> sort)
        {
            Sort = sort;
        }

        public SortDefinition<TInput> Sort { get; private set; }

        public override string OperatorName => "$sort";

        public override RenderedPipelineStageDefinition<TInput> Render(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            var renderedSort = Sort.Render(inputSerializer, serializerRegistry, linqProvider);
            var document = new BsonDocument(OperatorName, renderedSort);
            return new RenderedPipelineStageDefinition<TInput>(OperatorName, document, inputSerializer);
        }
    }
}
