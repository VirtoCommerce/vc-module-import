/**
 * JSON parse reviver that converts ISO date strings to Date objects.
 * This is needed because when using interface-based types (after NSwag migration),
 * dates are not automatically converted from strings to Date objects.
 */
declare function dateReviver(key: string, value: unknown): unknown;
export declare class AuthApiBase {
  authToken: string;
  /**
   * JSON parse reviver for converting date strings to Date objects.
   * Subclasses use this when parsing API responses.
   * The dateReviver function is defined in File.Header.liquid template.
   */
  protected jsonParseReviver: typeof dateReviver;
  protected constructor();
  getBaseUrl(defaultUrl: string, baseUrl: string): string;
  setAuthToken(token: string): void;
  protected transformOptions(options: any): Promise<any>;
}
export declare class ImportClient extends AuthApiBase {
  private http;
  private baseUrl;
  constructor(
    baseUrl?: string,
    http?: {
      fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    },
  );
  /**
   * @param body (optional)
   * @return OK
   */
  runImport(body?: ImportProfile | undefined): Promise<ImportPushNotification>;
  protected processRunImport(response: Response): Promise<ImportPushNotification>;
  /**
   * @param body (optional)
   * @return OK
   */
  cancelJob(body?: ImportCancellationRequest | undefined): Promise<void>;
  protected processCancelJob(response: Response): Promise<void>;
  /**
   * @param body (optional)
   * @return Success
   */
  resumeImport(body?: ImportResumeRequest | undefined): Promise<ImportPushNotification>;
  protected processResumeImport(response: Response): Promise<ImportPushNotification>;
  /**
   * @param body (optional)
   * @return OK
   */
  preview(body?: ImportProfile | undefined): Promise<ImportDataPreview>;
  protected processPreview(response: Response): Promise<ImportDataPreview>;
  /**
   * @param body (optional)
   * @return OK
   */
  validate(body?: ImportProfile | undefined): Promise<ValidationResult>;
  protected processValidate(response: Response): Promise<ValidationResult>;
  /**
   * @return OK
   */
  getImporters(): Promise<IDataImporter[]>;
  protected processGetImporters(response: Response): Promise<IDataImporter[]>;
  /**
   * @return OK
   */
  getImportProfileById(profileId: string): Promise<ImportProfile>;
  protected processGetImportProfileById(response: Response): Promise<ImportProfile>;
  /**
   * @param body (optional)
   * @return OK
   */
  createImportProfile(body?: ImportProfile | undefined): Promise<ImportProfile>;
  protected processCreateImportProfile(response: Response): Promise<ImportProfile>;
  /**
   * @param body (optional)
   * @return OK
   */
  updateImportProfile(body?: ImportProfile | undefined): Promise<ImportProfile>;
  protected processUpdateImportProfile(response: Response): Promise<ImportProfile>;
  /**
   * @param profileId (optional)
   * @return OK
   */
  deleteProfile(profileId?: string | undefined): Promise<void>;
  protected processDeleteProfile(response: Response): Promise<void>;
  /**
   * @param body (optional)
   * @return OK
   */
  searchImportProfiles(body?: SearchImportProfilesCriteria | undefined): Promise<SearchImportProfilesResult>;
  protected processSearchImportProfiles(response: Response): Promise<SearchImportProfilesResult>;
  /**
   * @param body (optional)
   * @return OK
   */
  searchImportRunHistory(body?: SearchImportRunHistoryCriteria | undefined): Promise<SearchImportRunHistoryResult>;
  protected processSearchImportRunHistory(response: Response): Promise<SearchImportRunHistoryResult>;
}
export declare class OrganizationClient extends AuthApiBase {
  private http;
  private baseUrl;
  constructor(
    baseUrl?: string,
    http?: {
      fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    },
  );
  /**
   * @param organizationId (optional)
   * @return OK
   */
  getOrganizationInfo(organizationId?: string | undefined): Promise<OrganizationInfo>;
  protected processGetOrganizationInfo(response: Response): Promise<OrganizationInfo>;
}
export interface IAuthorizationRequirement {}
export interface IDataImporter {
  readonly typeName?: string | undefined;
  readonly metadata?:
    | {
        [key: string]: string;
      }
    | undefined;
  availSettings?: SettingDescriptor[] | undefined;
  authorizationRequirement?: IAuthorizationRequirement | undefined;
}
export interface ImportCancellationRequest {
  jobId?: string | undefined;
}
export interface ImportResumeRequest {
  jobId?: string | undefined;
}
export interface ImportDataPreview {
  totalCount?: number;
  fileName?: string | undefined;
  records?: any[] | undefined;
  errors?: string[] | undefined;
}
export interface ImportProfile {
  name?: string | undefined;
  dataImporterType?: string | undefined;
  userId?: string | undefined;
  userName?: string | undefined;
  settings?: ObjectSettingEntry[] | undefined;
  readonly typeName?: string | undefined;
  profileType?: string | undefined;
  importFileUrl?: string | undefined;
  importReportUrl?: string | undefined;
  importReporterType?: string | undefined;
  previewObjectCount?: number;
  runHistory?: ImportRunHistory | undefined;
  createdDate?: Date;
  modifiedDate?: Date | undefined;
  createdBy?: string | undefined;
  modifiedBy?: string | undefined;
  id?: string | undefined;
}
export interface ImportPushNotification {
  profileId?: string | undefined;
  profileName?: string | undefined;
  jobId?: string | undefined;
  runId?: string | undefined;
  estimatingRemaining?: boolean;
  estimatedRemaining?: string | undefined;
  finished?: Date | undefined;
  totalCount?: number;
  processedCount?: number;
  readonly errorCount?: number;
  errors?: string[] | undefined;
  reportUrl?: string | undefined;
  serverId?: string | undefined;
  creator?: string | undefined;
  created?: Date;
  isNew?: boolean;
  notifyType?: string | undefined;
  description?: string | undefined;
  title?: string | undefined;
  repeatCount?: number;
  id?: string | undefined;
}
export interface ImportRunHistory {
  userId?: string | undefined;
  userName?: string | undefined;
  jobId?: string | undefined;
  profileId?: string | undefined;
  profileName?: string | undefined;
  name?: string | undefined;
  executed?: Date;
  finished?: Date | undefined;
  totalCount?: number;
  processedCount?: number;
  errorsCount?: number;
  errors?: string[] | undefined;
  fileUrl?: string | undefined;
  reportUrl?: string | undefined;
  cursor?: string | undefined;
  readonly typeName?: string | undefined;
  settings?: ObjectSettingEntry[] | undefined;
  createdDate?: Date;
  modifiedDate?: Date | undefined;
  createdBy?: string | undefined;
  modifiedBy?: string | undefined;
  id?: string | undefined;
}
export interface ObjectSettingEntry {
  readonly itHasValues?: boolean;
  objectId?: string | undefined;
  objectType?: string | undefined;
  isReadOnly?: boolean;
  value?: any | undefined;
  id?: string | undefined;
  restartRequired?: boolean;
  moduleId?: string | undefined;
  groupName?: string | undefined;
  name?: string | undefined;
  displayName?: string | undefined;
  isRequired?: boolean;
  isHidden?: boolean;
  isPublic?: boolean;
  valueType?: SettingValueType;
  allowedValues?: any[] | undefined;
  defaultValue?: any | undefined;
  isDictionary?: boolean;
  isLocalizable?: boolean;
}
export interface OrganizationInfo {
  organizationId?: string | undefined;
  organizationName?: string | undefined;
  organizationLogoUrl?: string | undefined;
}
export interface SearchImportProfilesCriteria {
  userId?: string | undefined;
  userName?: string | undefined;
  name?: string | undefined;
  dataImporterType?: string | undefined;
  responseGroup?: string | undefined;
  objectType?: string | undefined;
  objectTypes?: string[] | undefined;
  objectIds?: string[] | undefined;
  keyword?: string | undefined;
  searchPhrase?: string | undefined;
  languageCode?: string | undefined;
  sort?: string | undefined;
  readonly sortInfos?: SortInfo[] | undefined;
  skip?: number;
  take?: number;
}
export interface SearchImportProfilesResult {
  totalCount?: number;
  results?: ImportProfile[] | undefined;
}
export interface SearchImportRunHistoryCriteria {
  userId?: string | undefined;
  userName?: string | undefined;
  profileId?: string | undefined;
  profileIds?: string[] | undefined;
  jobId?: string | undefined;
  responseGroup?: string | undefined;
  objectType?: string | undefined;
  objectTypes?: string[] | undefined;
  objectIds?: string[] | undefined;
  keyword?: string | undefined;
  searchPhrase?: string | undefined;
  languageCode?: string | undefined;
  sort?: string | undefined;
  readonly sortInfos?: SortInfo[] | undefined;
  skip?: number;
  take?: number;
}
export interface SearchImportRunHistoryResult {
  totalCount?: number;
  results?: ImportRunHistory[] | undefined;
}
export interface SettingDescriptor {
  id?: string | undefined;
  restartRequired?: boolean;
  moduleId?: string | undefined;
  groupName?: string | undefined;
  name?: string | undefined;
  displayName?: string | undefined;
  isRequired?: boolean;
  isHidden?: boolean;
  isPublic?: boolean;
  valueType?: SettingValueType;
  allowedValues?: any[] | undefined;
  defaultValue?: any | undefined;
  isDictionary?: boolean;
  isLocalizable?: boolean;
}
export declare enum SettingValueType {
  ShortText = "ShortText",
  LongText = "LongText",
  Integer = "Integer",
  Decimal = "Decimal",
  DateTime = "DateTime",
  Boolean = "Boolean",
  SecureString = "SecureString",
  Json = "Json",
  PositiveInteger = "PositiveInteger",
}
export declare enum SortDirection {
  Ascending = "Ascending",
  Descending = "Descending",
}
export interface SortInfo {
  sortColumn?: string | undefined;
  sortDirection?: SortDirection;
}
export interface ValidationResult {
  errors?: string[] | undefined;
  readonly errorsCount?: number;
}
export declare class ApiException extends Error {
  message: string;
  status: number;
  response: string;
  headers: {
    [key: string]: any;
  };
  result: any;
  constructor(
    message: string,
    status: number,
    response: string,
    headers: {
      [key: string]: any;
    },
    result: any,
  );
  protected isApiException: boolean;
  static isApiException(obj: any): obj is ApiException;
}
export {};
//# sourceMappingURL=virtocommerce.import.d.ts.map
