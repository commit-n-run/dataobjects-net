// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.11.18

using Xtensive.Orm.Configuration;
using Xtensive.Sql;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// A handlers that is capable of creating <see cref="CommandProcessor"/>s.
  /// </summary>
  public class CommandProcessorFactory : DomainBoundHandler
  {
    private StorageDriver driver;
    private ProviderInfo providerInfo;

    /// <summary>
    /// Creates the command processor.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="connection">The connection.</param>
    /// <returns>Created command processor.</returns>
    public CommandProcessor CreateCommandProcessor(Session session, SqlConnection connection)
    {
      var configuration = session.Configuration;
      var useBatches = configuration.BatchSize > 1
        && providerInfo.Supports(ProviderFeatures.DmlBatches);
      var useCursorParameters =
        providerInfo.Supports(ProviderFeatures.MultipleResultsViaCursorParameters);

      var factory = useCursorParameters
        ? new CursorCommandFactory(driver, session, connection)
        : new CommandFactory(driver, session, connection);

      var processor = useBatches
        ? new BatchingCommandProcessor(factory, configuration.BatchSize, session.Domain.StorageProviderInfo.MaxQueryParameterCount)
        : (CommandProcessor) new SimpleCommandProcessor(factory, session.Domain.StorageProviderInfo.MaxQueryParameterCount);
      return processor;
      
    }

    /// <inheritdoc/>
    protected override void Initialize()
    {
      driver = Handlers.StorageDriver;
      providerInfo = Handlers.ProviderInfo;
    }
  }
}